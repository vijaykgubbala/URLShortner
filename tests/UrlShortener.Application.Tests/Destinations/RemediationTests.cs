using System.Net;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Tests.Destinations;

/// <summary>Tests added by the review remediation pass. See docs/reviews/2026-08-17-17.json.</summary>
public class RemediationTests
{
    private sealed class RecordingLogger : ILogger<ValidateDestination>
    {
        public List<string> Events { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel l, EventId id, TState s, Exception? e,
            Func<TState, Exception?, string> f) => Events.Add(id.Name ?? string.Empty);
    }

    private sealed class CountingResolver : IHostResolver
    {
        public int Calls { get; private set; }
        public Task<HostResolution> ResolveAsync(string host, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult<HostResolution>(
                new HostResolution.Resolved([IPAddress.Parse("93.184.216.34")]));
        }
    }

    // ---- TST-001: AC-5 on the scheme and parse rejection path ----

    /// <summary>
    /// The early-return branch emitted telemetry that nothing asserted. Deleting the
    /// Record call kept all 93 tests green while removing observability from the
    /// rejection class that carries probing traffic.
    /// </summary>
    [Theory]
    [InlineData("javascript:alert(1)", DestinationRefusal.SchemeNotPermitted)]
    [InlineData("data:text/html,x", DestinationRefusal.SchemeNotPermitted)]
    [InlineData("not-a-url", DestinationRefusal.NotAbsoluteUrl)]
    [InlineData("", DestinationRefusal.NotAbsoluteUrl)]
    public async Task A_scheme_or_parse_rejection_emits_the_event_and_increments_the_counter(
        string raw, DestinationRefusal expected)
    {
        var logger = new RecordingLogger();
        var counter = new RejectionCounter();
        var resolver = new CountingResolver();

        var result = await new ValidateDestination(resolver, logger, counter)
            .ExecuteAsync(raw, CancellationToken.None);

        Assert.Equal(expected, result.Refusal);
        Assert.Contains("link.destination.rejected", logger.Events);
        Assert.Equal(1, counter.For(expected));
        Assert.Equal(0, resolver.Calls);
    }

    // ---- SEC-003: STD-SEC-02 requires a length bound at the boundary ----

    /// <summary>
    /// The rule requires type, range, length and format. Only type and format existed,
    /// so an unbounded string reached DNS resolution and the log.
    /// </summary>
    [Fact]
    public async Task An_over_length_destination_is_refused_without_resolving_anything()
    {
        var resolver = new CountingResolver();
        var oversized = "https://example.com/" + new string('a', 4096);

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync(oversized, CancellationToken.None);

        Assert.False(result.IsPermitted);
        Assert.Equal(DestinationRefusal.NotAbsoluteUrl, result.Refusal);
        Assert.Equal(0, resolver.Calls);
    }

    [Fact]
    public async Task A_destination_at_the_length_limit_is_still_evaluated()
    {
        var resolver = new CountingResolver();
        var prefix = "https://example.com/";
        var atLimit = prefix + new string('a', ValidateDestination.MaxDestinationLength - prefix.Length);

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync(atLimit, CancellationToken.None);

        Assert.True(result.IsPermitted);
        Assert.Equal(1, resolver.Calls);
    }

    // ---- COR-003: the counter is shared and mutated concurrently ----

    /// <summary>
    /// Non-atomic Total++ and an unsynchronised Dictionary on a necessarily-singleton
    /// instance. Beyond lost counts, concurrent resize can spin a request thread inside
    /// FindEntry — a hang on the creation path caused by telemetry, which STD-OPS-04
    /// forbids at critical severity.
    /// </summary>
    [Fact]
    public void The_counter_survives_concurrent_increments()
    {
        var counter = new RejectionCounter();

        Parallel.For(0, 2000, i => counter.Increment(
            i % 2 == 0 ? DestinationRefusal.AddressNotPermitted : DestinationRefusal.ResolutionFailed));

        Assert.Equal(2000, counter.Total);
        Assert.Equal(1000, counter.For(DestinationRefusal.AddressNotPermitted));
        Assert.Equal(1000, counter.For(DestinationRefusal.ResolutionFailed));
    }
}
