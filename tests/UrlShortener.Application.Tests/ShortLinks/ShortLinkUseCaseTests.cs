using System.Net;
using UrlShortener.Application.Destinations;
using UrlShortener.Application.ShortLinks;
using UrlShortener.Domain.Destinations;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Application.Tests.ShortLinks;

/// <summary>
/// #18 and #19 at the orchestration level — the cases that are hard to force through HTTP:
/// a code collision, and exhaustion of the retry budget.
/// </summary>
public class ShortLinkUseCaseTests
{
    private sealed class PermittingResolver : IHostResolver
    {
        public Task<HostResolution> ResolveAsync(string host, CancellationToken ct) =>
            Task.FromResult<HostResolution>(new HostResolution.Resolved([IPAddress.Parse("93.184.216.34")]));
    }

    /// <summary>An in-memory stand-in for the unique constraint.</summary>
    private sealed class FakeRepository : IShortLinkRepository
    {
        private readonly Dictionary<string, ShortLink> _links = [];

        public int AddAttempts { get; private set; }

        public Task<bool> TryAddAsync(ShortLink link, CancellationToken ct)
        {
            AddAttempts++;
            return Task.FromResult(_links.TryAdd(link.Code, link));
        }

        public Task<ShortLink?> FindAsync(string code, CancellationToken ct) =>
            Task.FromResult(_links.GetValueOrDefault(code));

        public void Seed(ShortLink link) => _links[link.Code] = link;
    }

    /// <summary>Hands out a fixed sequence, so a collision can be forced exactly.</summary>
    private sealed class ScriptedGenerator(params string[] codes) : IShortCodeGenerator
    {
        private int _next;

        public string Next() => codes[Math.Min(_next++, codes.Length - 1)];
    }

    private static CreateShortLink Creator(
        FakeRepository repository, IShortCodeGenerator codes, CreateFailureCounter? counter = null) =>
        new(repository, codes, new ValidateDestination(new PermittingResolver()),
            TimeProvider.System, null, counter);

    [Fact]
    public async Task A_colliding_code_is_retried_rather_than_returned_as_a_failure()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow));

        var result = await Creator(repository, new ScriptedGenerator("TAKEN01", "FREE001"))
            .ExecuteAsync("https://example.com/second", CancellationToken.None);

        Assert.Equal(CreateOutcome.Created, result.Outcome);
        Assert.Equal("FREE001", result.Code);
        Assert.Equal(2, repository.AddAttempts);
    }

    [Fact]
    public async Task Exhausting_the_retry_budget_reports_failure_and_increments_the_counter()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow));
        var counter = new CreateFailureCounter();

        var result = await Creator(repository, new ScriptedGenerator("TAKEN01"), counter)
            .ExecuteAsync("https://example.com/second", CancellationToken.None);

        Assert.Equal(CreateOutcome.CodeExhausted, result.Outcome);
        Assert.Null(result.Code);
        Assert.Equal(1, counter.Total);
    }

    [Fact]
    public async Task A_refused_destination_never_reaches_the_repository()
    {
        var repository = new FakeRepository();

        var result = await Creator(repository, new ScriptedGenerator("FREE001"))
            .ExecuteAsync("javascript:alert(1)", CancellationToken.None);

        Assert.Equal(CreateOutcome.DestinationRefused, result.Outcome);
        Assert.Equal(0, repository.AddAttempts);
    }

    [Fact]
    public async Task An_unknown_code_resolves_to_NotFound()
    {
        var result = await new ResolveShortLink(new FakeRepository())
            .ExecuteAsync("MISSING", CancellationToken.None);

        Assert.Equal(ResolveOutcome.NotFound, result.Outcome);
    }

    /// <summary>
    /// WVR-001 waives the address half of the redirect-path re-check, not the scheme half.
    /// This pins the half that is implemented, so the waiver expiring is what adds coverage
    /// rather than what discovers its absence.
    /// </summary>
    [Fact]
    public async Task A_stored_destination_the_policy_now_refuses_is_not_handed_back()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("LEGACY1", "file:///etc/passwd", DateTimeOffset.UtcNow));

        var result = await new ResolveShortLink(repository)
            .ExecuteAsync("LEGACY1", CancellationToken.None);

        Assert.Equal(ResolveOutcome.NoLongerPermitted, result.Outcome);
        Assert.Null(result.Destination);
    }
}
