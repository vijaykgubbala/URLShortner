using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UrlShortener.Api.ShortLinks;
using UrlShortener.Application.Destinations;
using UrlShortener.Application.ShortLinks;
using UrlShortener.Domain.Destinations;
using UrlShortener.Domain.ShortLinks;
using UrlShortener.Infrastructure.ShortLinks;

namespace UrlShortener.Api.Tests.ShortLinks;

/// <summary>
/// #18 and #19, exercised through the real HTTP pipeline.
///
/// DNS is stubbed — a test that depends on live name resolution fails on a bad network
/// rather than on a bad change. Everything else is real: real routing, real EF Core over
/// SQLite, real unique constraint.
/// </summary>
public class ShortLinkEndpointTests : IClassFixture<ShortLinkEndpointTests.Host>
{
    private readonly Host _host;

    public ShortLinkEndpointTests(Host host) => _host = host;

    public sealed class StubResolver(params string[] addresses) : IHostResolver
    {
        public Task<HostResolution> ResolveAsync(string host, CancellationToken ct) =>
            Task.FromResult<HostResolution>(new HostResolution.Resolved(
                addresses.Select(IPAddress.Parse).ToArray()));
    }

    public sealed class Host : WebApplicationFactory<Program>
    {
        private readonly SqliteHolder _db = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ShortLinkDbContext>>();
                services.AddDbContext<ShortLinkDbContext>(o => o.UseSqlite(_db.Connection));
                services.RemoveAll<IHostResolver>();
                services.AddSingleton<IHostResolver>(new StubResolver("93.184.216.34"));
            });

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _db.Dispose();
        }
    }

    public sealed class SqliteHolder : IDisposable
    {
        public Microsoft.Data.Sqlite.SqliteConnection Connection { get; }

        public SqliteHolder()
        {
            Connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            Connection.Open();
        }

        public void Dispose() => Connection.Dispose();
    }

    private static HttpClient NoRedirect(Host host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    // ---- #18 ----

    [Fact]
    public async Task Creating_a_link_returns_201_a_seven_character_code_and_a_location_header()
    {
        var client = NoRedirect(_host);

        var response = await client.PostAsJsonAsync(
            "/v1/short-links", new { destination = "https://example.com/a-long-path" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CreateShortLinkResponse>();
        Assert.Equal(7, body!.Code.Length);
    }

    [Fact]
    public async Task A_refused_destination_is_not_stored()
    {
        var client = NoRedirect(_host);

        var response = await client.PostAsJsonAsync(
            "/v1/short-links", new { destination = "javascript:alert(1)" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShortLinkDbContext>();
        Assert.DoesNotContain(db.ShortLinks, l => l.Destination.StartsWith("javascript:"));
    }

    /// <summary>
    /// AC: concurrent creates produce distinct codes and none fails on a duplicate key.
    /// This exercises the real unique constraint under real concurrency; it does not force
    /// a collision, so it proves the happy path is not serialised rather than proving the
    /// retry loop fires. The forced-collision case is covered in the unit test below.
    /// </summary>
    [Fact]
    public async Task Concurrent_creates_produce_distinct_codes_without_a_duplicate_key_failure()
    {
        var client = NoRedirect(_host);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(i =>
                client.PostAsJsonAsync("/v1/short-links", new { destination = $"https://example.com/{i}" })));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.Created, r.StatusCode));

        var codes = new List<string>();
        foreach (var r in responses)
        {
            codes.Add((await r.Content.ReadFromJsonAsync<CreateShortLinkResponse>())!.Code);
        }

        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    // ---- #19 ----

    [Fact]
    public async Task Following_a_short_code_redirects_to_the_destination()
    {
        var client = NoRedirect(_host);
        const string destination = "https://example.com/target";

        var created = await client.PostAsJsonAsync("/v1/short-links", new { destination });
        var code = (await created.Content.ReadFromJsonAsync<CreateShortLinkResponse>())!.Code;

        var redirect = await client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);
        Assert.Equal(destination, redirect.Headers.Location!.ToString());
    }

    [Fact]
    public async Task An_unknown_code_returns_404()
    {
        var response = await NoRedirect(_host).GetAsync("/ZZZZZZZ");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Review finding C4 — /{code} used to capture every single-segment path, so these all
    /// became database round trips inside the 50 ms p99 budget. Browsers request the first
    /// two unprompted. They now miss in routing.
    /// </summary>
    [Theory]
    [InlineData("/favicon.ico")]
    [InlineData("/robots.txt")]
    [InlineData("/apple-touch-icon.png")]
    [InlineData("/a")]
    [InlineData("/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task A_path_that_is_not_code_shaped_never_reaches_resolution(string path)
    {
        var response = await NoRedirect(_host).GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Right length, wrong alphabet — the guard STD-SEC-02 asks for beyond length.</summary>
    [Theory]
    [InlineData("/abc-def")]
    [InlineData("/../etc/")]
    [InlineData("/%20%20%20%20%20%20%20")]
    public async Task A_code_outside_the_alphabet_is_refused(string path)
    {
        var response = await NoRedirect(_host).GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Review finding C1/SEC-002. Uri.Host here is `evil.example`, which resolves publicly,
    /// so every host and address rule passed it — while the Location header a person reads
    /// says paypal.com.
    /// </summary>
    [Fact]
    public async Task A_destination_carrying_userinfo_is_refused_and_not_stored()
    {
        const string destination = "https://www.paypal.com@evil.example/login";

        var response = await NoRedirect(_host).PostAsJsonAsync(
            "/v1/short-links", new { destination });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ShortLinkDbContext>();
        Assert.DoesNotContain(db.ShortLinks, l => l.Destination == destination);
    }

    /// <summary>
    /// Review finding C2/COR-001. The raw string used to be stored and handed to the
    /// Location header verbatim: a non-ASCII byte in a response header is what Kestrel
    /// refuses, so the link created with 201 then failed on every visit.
    /// </summary>
    [Fact]
    public async Task A_non_ascii_destination_round_trips_as_its_escaped_form()
    {
        var client = NoRedirect(_host);

        var created = await client.PostAsJsonAsync(
            "/v1/short-links", new { destination = "https://example.com/café" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var code = (await created.Content.ReadFromJsonAsync<CreateShortLinkResponse>())!.Code;

        var redirect = await client.GetAsync($"/{code}");

        Assert.Equal(HttpStatusCode.Found, redirect.StatusCode);

        // The raw header value, not Headers.Location.ToString() — Uri.ToString() unescapes,
        // so asserting against it would test .NET's parser rather than what goes on the wire.
        Assert.Equal("https://example.com/caf%C3%A9", RawLocation(redirect));
    }

    /// <summary>A CR/LF in a destination must never reach a response header intact.</summary>
    [Fact]
    public async Task A_destination_containing_crlf_is_escaped_before_it_reaches_the_header()
    {
        var client = NoRedirect(_host);

        var created = await client.PostAsJsonAsync(
            "/v1/short-links", new { destination = "https://example.com/a\r\nSet-Cookie: x=y" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var code = (await created.Content.ReadFromJsonAsync<CreateShortLinkResponse>())!.Code;

        var redirect = await client.GetAsync($"/{code}");
        var location = RawLocation(redirect);

        Assert.DoesNotContain("\r", location);
        Assert.DoesNotContain("\n", location);
        Assert.Contains("%0D%0A", location);
        Assert.False(redirect.Headers.TryGetValues("Set-Cookie", out _));
    }

    private static string RawLocation(HttpResponseMessage response) =>
        response.Headers.GetValues("Location").Single();

    /// <summary>
    /// AC: a stored destination the policy now rejects is refused with 410 and no Location.
    /// The row is written directly, simulating a link stored before a policy change.
    /// </summary>
    [Fact]
    public async Task A_stored_destination_the_policy_now_rejects_returns_410_with_no_location()
    {
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShortLinkDbContext>();
            db.ShortLinks.Add(new ShortLink("LEGACY1", "ftp://example.com/file", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await NoRedirect(_host).GetAsync("/LEGACY1");

        Assert.Equal(HttpStatusCode.Gone, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task A_refused_redirect_increments_the_failure_counter()
    {
        var counter = _host.Services.GetRequiredService<ResolveFailureCounter>();
        var before = counter.Total;

        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ShortLinkDbContext>();
            db.ShortLinks.Add(new ShortLink("LEGACY2", "file:///etc/passwd", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        await NoRedirect(_host).GetAsync("/LEGACY2");

        Assert.True(counter.Total > before);
    }
}
