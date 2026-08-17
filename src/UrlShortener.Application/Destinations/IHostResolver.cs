using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Destinations;

/// <summary>
/// What the use case needs from the outside world in order to judge a destination.
///
/// Declared in Application per <c>layers.md §1.3</c>; returns a Domain type per ADR-001,
/// because the Domain predicate takes that type as a parameter and Domain may reference
/// nothing (<c>§2.1</c>).
///
/// Returns an outcome rather than throwing. Resolving an arbitrary user-supplied hostname
/// fails routinely, and a thrown failure would force the caller into a catch block that
/// decides whether to reject — a business rule, which §1.3 keeps out of Application.
/// </summary>
public interface IHostResolver
{
    Task<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken);
}
