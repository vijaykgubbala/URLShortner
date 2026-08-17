using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace UrlShortener.Application.Destinations;

/// <summary>
/// Counts destination refusals, by kind.
///
/// <c>STD-OPS-02</c> requires the metric as an acceptance criterion rather than follow-up
/// work. Counting by kind matters operationally: a rise in <see
/// cref="DestinationRefusal.AddressNotPermitted"/> is someone probing the internal
/// network, while a rise in <see cref="DestinationRefusal.ResolutionFailed"/> is our own
/// resolver in trouble. A single total cannot tell those apart.
///
/// The state is synchronised because the only sensible lifetime for this type is a
/// singleton — <see cref="Total"/> and <see cref="For"/> are meaningless per-request, and
/// the <see cref="Meter"/> beside them is static. An unsynchronised Dictionary here is not
/// merely a lost count: two threads resizing it concurrently can corrupt the bucket chain
/// and spin a request thread indefinitely, which turns telemetry into a hang on the
/// creation path — the failure <c>STD-OPS-04</c> exists to prevent.
/// </summary>
public sealed class RejectionCounter
{
    public const string MeterName = "UrlShortener.Destinations";
    public const string CounterName = "link_destination_rejections";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Emitted = Meter.CreateCounter<long>(CounterName);

    private readonly ConcurrentDictionary<DestinationRefusal, int> _byKind = new();
    private int _total;

    public int Total => Volatile.Read(ref _total);

    public void Increment(DestinationRefusal refusal)
    {
        Interlocked.Increment(ref _total);
        _byKind.AddOrUpdate(refusal, 1, static (_, existing) => existing + 1);
        Emitted.Add(1, new KeyValuePair<string, object?>("reason", refusal.ToString()));
    }

    public int For(DestinationRefusal refusal) =>
        _byKind.TryGetValue(refusal, out var count) ? count : 0;
}
