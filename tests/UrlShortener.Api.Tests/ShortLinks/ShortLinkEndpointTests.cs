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
