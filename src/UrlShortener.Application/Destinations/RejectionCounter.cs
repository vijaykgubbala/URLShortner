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
/// </summary>
public sealed class RejectionCounter
{
    public const string MeterName = "UrlShortener.Destinations";
    public const string CounterName = "link_destination_rejections";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> Emitted = Meter.CreateCounter<long>(CounterName);

    private readonly Dictionary<DestinationRefusal, int> _byKind = [];

    public int Total { get; private set; }

    public void Increment(DestinationRefusal refusal)
    {
        Total++;
        _byKind[refusal] = For(refusal) + 1;
        Emitted.Add(1, new KeyValuePair<string, object?>("reason", refusal.ToString()));
    }

    public int For(DestinationRefusal refusal) =>
        _byKind.TryGetValue(refusal, out var count) ? count : 0;
}
