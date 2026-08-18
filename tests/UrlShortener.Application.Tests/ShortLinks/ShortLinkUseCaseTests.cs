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

        public Task<bool> TryDeleteAsync(string code, CancellationToken ct) =>
            Task.FromResult(_links.Remove(code));

        public void Seed(ShortLink link) => _links[link.Code] = link;
    }

    /// <summary>A fixed token, so a test can assert what the create returned.</summary>
    private sealed class FixedTokenGenerator(string token) : ILinkTokenGenerator
    {
        public static readonly string Token =
            System.Buffers.Text.Base64Url.EncodeToString(Enumerable.Repeat((byte)0x11, 32).ToArray());

        public string Next() => token;
    }

    /// <summary>
    /// T-18. Counts verifications so a test can prove the use case reached the check even
    /// when the code did not exist. An early return there is behaviourally identical --
    /// same outcome, same body -- and only differs in duration, so this seam is the only
    /// thing that can catch its absence.
    /// </summary>
    private sealed class CountingVerifier : ILinkTokenVerifier
    {
        public int Calls { get; private set; }

        public bool Verify(string? presented, byte[]? storedHash)
        {
            Calls++;
            return LinkToken.Verify(presented, storedHash);
        }
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
        new(repository, codes, new FixedTokenGenerator(FixedTokenGenerator.Token),
            new ValidateDestination(new PermittingResolver()),
            TimeProvider.System, logger, counter);

    [Fact]
    public async Task A_colliding_code_is_retried_rather_than_returned_as_a_failure()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow, null));

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
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow, null));
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
        repository.Seed(new ShortLink("TAKEN01", "https://example.com/first", DateTimeOffset.UtcNow, null));
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
        repository.Seed(new ShortLink("LEGACY1", "file:///etc/passwd", DateTimeOffset.UtcNow, null));
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
        repository.Seed(new ShortLink("GOOD001", "https://example.com/ok", DateTimeOffset.UtcNow, null));
        var logger = new RecordingLogger<ResolveShortLink>();
        var counter = new ResolveFailureCounter();

        var result = await new ResolveShortLink(repository, logger, counter)
            .ExecuteAsync("GOOD001", CancellationToken.None);

        Assert.Equal(ResolveOutcome.Found, result.Outcome);
        Assert.Empty(logger.Events);
        Assert.Equal(0, counter.Total);
    }
}

/// <summary>#21 — the delete path and its authorization.</summary>
public class DeleteShortLinkTests
{
    private static readonly string Token =
        System.Buffers.Text.Base64Url.EncodeToString(Enumerable.Repeat((byte)0x2B, 32).ToArray());

    private static readonly string Forged =
        System.Buffers.Text.Base64Url.EncodeToString(Enumerable.Repeat((byte)0x2C, 32).ToArray());

    private sealed class FakeRepository : IShortLinkRepository
    {
        private readonly Dictionary<string, ShortLink> _links = [];

        public Task<bool> TryAddAsync(ShortLink link, CancellationToken ct)
        {
            _links[link.Code] = link;
            return Task.FromResult(true);
        }

        public Task<ShortLink?> FindAsync(string code, CancellationToken ct) =>
            Task.FromResult(_links.GetValueOrDefault(code));

        public Task<bool> TryDeleteAsync(string code, CancellationToken ct) =>
            Task.FromResult(_links.Remove(code));

        public bool Has(string code) => _links.ContainsKey(code);

        public void Seed(ShortLink link) => _links[link.Code] = link;
    }

    private sealed class CountingVerifier : ILinkTokenVerifier
    {
        public int Calls { get; private set; }

        public bool Verify(string? presented, byte[]? storedHash)
        {
            Calls++;
            return LinkToken.Verify(presented, storedHash);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }

    private static FakeRepository SeededRepository()
    {
        var repository = new FakeRepository();
        repository.Seed(new ShortLink(
            "OWNED01", "https://example.com/mine", DateTimeOffset.UtcNow, LinkToken.Hash(Token)));
        return repository;
    }

    [Fact]
    public async Task The_correct_token_deletes_the_link()
    {
        var repository = SeededRepository();

        var result = await new DeleteShortLink(repository, new CountingVerifier())
            .ExecuteAsync("OWNED01", Token, CancellationToken.None);

        Assert.Equal(DeleteOutcome.Deleted, result.Outcome);
        Assert.False(repository.Has("OWNED01"));
    }

    /// <summary>T-17 — the forged-token negative test STD-SEC-06 requires.</summary>
    [Fact]
    public async Task A_forged_token_is_refused_and_the_link_survives()
    {
        var repository = SeededRepository();

        var result = await new DeleteShortLink(repository, new CountingVerifier())
            .ExecuteAsync("OWNED01", Forged, CancellationToken.None);

        Assert.Equal(DeleteOutcome.Refused, result.Outcome);
        Assert.True(repository.Has("OWNED01"));
    }

    [Fact]
    public async Task A_missing_token_is_refused_and_the_link_survives()
    {
        var repository = SeededRepository();

        var result = await new DeleteShortLink(repository, new CountingVerifier())
            .ExecuteAsync("OWNED01", null, CancellationToken.None);

        Assert.Equal(DeleteOutcome.Refused, result.Outcome);
        Assert.True(repository.Has("OWNED01"));
    }

    /// <summary>
    /// An unknown code and a wrong token must be indistinguishable to the caller, so the
    /// use case reports the same outcome for both. ADR-002.
    /// </summary>
    [Fact]
    public async Task An_unknown_code_reports_the_same_outcome_as_a_wrong_token()
    {
        var unknown = await new DeleteShortLink(SeededRepository(), new CountingVerifier())
            .ExecuteAsync("NOSUCH1", Token, CancellationToken.None);

        var wrong = await new DeleteShortLink(SeededRepository(), new CountingVerifier())
            .ExecuteAsync("OWNED01", Forged, CancellationToken.None);

        Assert.Equal(wrong.Outcome, unknown.Outcome);
    }

    /// <summary>
    /// T-18 — the test the ILinkTokenVerifier seam exists for. Verification must run even
    /// when the code does not exist, or an unknown code returns measurably faster than a
    /// wrong token and timing discloses what the identical 404 conceals.
    /// </summary>
    [Fact]
    public async Task Verification_runs_even_when_the_code_does_not_exist()
    {
        var verifier = new CountingVerifier();

        await new DeleteShortLink(SeededRepository(), verifier)
            .ExecuteAsync("NOSUCH1", Token, CancellationToken.None);

        Assert.Equal(1, verifier.Calls);
    }

    /// <summary>T-16 — AC-5. The token appears in no log argument, on any path.</summary>
    [Fact]
    public async Task The_token_never_reaches_a_log_message()
    {
        var logger = new RecordingLogger<DeleteShortLink>();
        var sut = new DeleteShortLink(SeededRepository(), new CountingVerifier(), logger);

        await sut.ExecuteAsync("OWNED01", Token, CancellationToken.None);
        await sut.ExecuteAsync("OWNED01", Forged, CancellationToken.None);
        await sut.ExecuteAsync("NOSUCH1", Token, CancellationToken.None);

        Assert.DoesNotContain(logger.Messages, m => m.Contains(Token));
        Assert.DoesNotContain(logger.Messages, m => m.Contains(Forged));
    }
}
