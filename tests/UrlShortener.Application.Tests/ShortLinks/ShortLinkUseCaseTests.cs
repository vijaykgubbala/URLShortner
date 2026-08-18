using System.Net;
using Microsoft.Extensions.Logging;
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

    /// <summary>
    /// Captures the structured event name, matching the pattern already used by
    /// RejectionTelemetryTests for #17. Review findings C7/C8 — both telemetry ACs are
    /// conjunctions ("emits X *and* increments Y") and only the counter half was asserted,
    /// so either log call could be deleted with every test still green.
    /// </summary>
    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Events { get; } = [];
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Events.Add(eventId.Name ?? string.Empty);
            Messages.Add(formatter(state, exception));
        }
    }

    private static CreateShortLink Creator(
        FakeRepository repository,
        IShortCodeGenerator codes,
        CreateFailureCounter? counter = null,
        ILogger<CreateShortLink>? logger = null) =>
        new(repository, codes, new ValidateDestination(new PermittingResolver()),
            TimeProvider.System, logger, counter);

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

    /// <summary>C8 — AC #18.5's log half. Deleting the LogError left every test green.</summary>
    [Fact]
    public async Task Exhausting_the_retry_budget_emits_link_create_failed()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow));
        var logger = new RecordingLogger<CreateShortLink>();

        await Creator(repository, new ScriptedGenerator("TAKEN01"), logger: logger)
            .ExecuteAsync("https://example.com/second", CancellationToken.None);

        Assert.Single(logger.Events, e => e == "link.create.failed");
    }

    /// <summary>The negative twin — without it the event could be made unconditional.</summary>
    [Fact]
    public async Task A_successful_create_emits_no_failure_event()
    {
        var logger = new RecordingLogger<CreateShortLink>();

        await Creator(new FakeRepository(), new ScriptedGenerator("FREE001"), logger: logger)
            .ExecuteAsync("https://example.com/ok", CancellationToken.None);

        Assert.Empty(logger.Events);
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
        var logger = new RecordingLogger<ResolveShortLink>();
        var counter = new ResolveFailureCounter();

        var result = await new ResolveShortLink(repository, logger, counter)
            .ExecuteAsync("LEGACY1", CancellationToken.None);

        Assert.Equal(ResolveOutcome.NoLongerPermitted, result.Outcome);
        Assert.Null(result.Destination);

        // C7 — AC #19.5 is a conjunction. Both halves, exactly once each.
        Assert.Single(logger.Events, e => e == "redirect.resolve.failed");
        Assert.Equal(1, counter.Total);
    }

    /// <summary>The negative twin for AC #19.5.</summary>
    [Fact]
    public async Task A_successful_resolve_emits_no_failure_event_and_moves_no_counter()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("GOOD001", "https://example.com/ok", DateTimeOffset.UtcNow));
        var logger = new RecordingLogger<ResolveShortLink>();
        var counter = new ResolveFailureCounter();

        var result = await new ResolveShortLink(repository, logger, counter)
            .ExecuteAsync("GOOD001", CancellationToken.None);

        Assert.Equal(ResolveOutcome.Found, result.Outcome);
        Assert.Empty(logger.Events);
        Assert.Equal(0, counter.Total);
    }
}
