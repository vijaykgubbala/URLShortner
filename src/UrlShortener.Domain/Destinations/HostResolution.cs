using System.Net;

namespace UrlShortener.Domain.Destinations;

/// <summary>
/// What is known about where a destination host points.
///
/// Declared in Domain, not Application, because the Domain predicate takes it as a
/// parameter and <c>layers.md §2.1</c> says "Domain references no other project in this
/// system". The port that produces it lives in Application per §1.3; the type it returns
/// lives here. See ADR-001.
///
/// The failure cases are values rather than exceptions: resolving an arbitrary
/// user-supplied hostname fails routinely, and a thrown timeout would put the
/// accept-or-reject decision inside a catch block in the Application layer, which is a
/// business rule §1.3 keeps out of there.
/// </summary>
public abstract record HostResolution
{
    private HostResolution() { }

    /// <summary>The host resolved. Carries every address returned, unfiltered and unranked.</summary>
    public sealed record Resolved(IReadOnlyList<IPAddress> Addresses) : HostResolution;

    /// <summary>The host does not exist, or returned no address records.</summary>
    public sealed record NotFound : HostResolution;

    /// <summary>Resolution could not be completed — timeout, resolver error, network failure.</summary>
    public sealed record Failed : HostResolution;
}
