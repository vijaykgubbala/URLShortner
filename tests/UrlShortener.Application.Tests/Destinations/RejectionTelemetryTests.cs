using System.Net;
using Microsoft.Extensions.Logging;
using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Tests.Destinations;

/// <summary>
/// T-24, T-25 — a refusal is visible in operations without leaking into the response.
///
/// STD-OPS-02 requires the log event and the metric as acceptance criteria rather than
/// follow-up work. api.md §4.3 keeps the resolved address out of the response body — so
/// the log is the only place it exists, correlated by traceId.
/// </summary>
public class RejectionTelemetryTests
{
    private sealed class RecordingLogger : ILogger<ValidateDestination>
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

    private sealed class StubResolver(HostResolution outcome) : IHostResolver
    {
        public Task<HostResolution> ResolveAsync(string host, CancellationToken ct) =>
            Task.FromResult(outcome);
    }

    private static (RecordingLogger Logger, RejectionCounter Counter, ValidateDestination Sut)
        Build(HostResolution outcome)
    {
        var logger = new RecordingLogger();
        var counter = new RejectionCounter();
        return (logger, counter, new ValidateDestination(new StubResolver(outcome), logger, counter));
    }

    // T-24
    [Fact]
    public async Task A_refusal_emits_the_event_and_increments_the_counter()
    {
        var (logger, counter, sut) = Build(new HostResolution.Resolved(
            [IPAddress.Parse("10.0.0.5")]));

        await sut.ExecuteAsync("https://internal.example", CancellationToken.None);

        Assert.Contains("link.destination.rejected", logger.Events);
        Assert.Equal(1, counter.Total);
    }

    [Fact]
    public async Task A_permitted_destination_emits_nothing_and_increments_nothing()
    {
        var (logger, counter, sut) = Build(new HostResolution.Resolved(
            [IPAddress.Parse("93.184.216.34")]));

        await sut.ExecuteAsync("https://example.com", CancellationToken.None);

        Assert.DoesNotContain("link.destination.rejected", logger.Events);
        Assert.Equal(0, counter.Total);
    }

    // T-24 — one event per refusal, not one per rule evaluated
    [Fact]
    public async Task A_refusal_emits_exactly_one_event()
    {
        var (logger, counter, sut) = Build(new HostResolution.Resolved(
            [IPAddress.Parse("10.0.0.5"), IPAddress.Parse("127.0.0.1"), IPAddress.Parse("192.168.1.1")]));

        await sut.ExecuteAsync("https://internal.example", CancellationToken.None);

        Assert.Single(logger.Events.Where(e => e == "link.destination.rejected"));
        Assert.Equal(1, counter.Total);
    }

    // T-25 — the address is in the log, and only in the log
    [Fact]
    public async Task The_refused_address_appears_in_the_log()
    {
        var (logger, _, sut) = Build(new HostResolution.Resolved([IPAddress.Parse("10.0.0.5")]));

        await sut.ExecuteAsync("https://internal.example", CancellationToken.None);

        Assert.Contains(logger.Messages, m => m.Contains("10.0.0.5"));
    }

    [Fact]
    public async Task The_refusal_reason_is_counted_by_kind()
    {
        var (_, counter, sut) = Build(new HostResolution.Failed());

        await sut.ExecuteAsync("https://timeout.example", CancellationToken.None);

        Assert.Equal(1, counter.For(DestinationRefusal.ResolutionFailed));
        Assert.Equal(0, counter.For(DestinationRefusal.AddressNotPermitted));
    }
}
