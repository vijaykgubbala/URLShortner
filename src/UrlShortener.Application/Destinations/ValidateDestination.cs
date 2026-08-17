using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Destinations;

/// <summary>Application's own vocabulary for a refusal. Deliberately not the Domain enum.</summary>
public enum DestinationRefusal
{
    None = 0,
    NotAbsoluteUrl,
    SchemeNotPermitted,
    HostNotResolved,
    ResolutionFailed,
    AddressNotPermitted
}

/// <summary>
/// The Application-declared result of validating a destination.
///
/// Exists so Entrypoints never maps a Domain type onto the published HTTP contract
/// (<c>layers.md §5.2</c>, <c>§5.3</c>). Without it, renaming a Domain enum member would
/// silently change a wire contract that <c>api.md §4.2</c> says must not change once
/// published.
/// </summary>
public sealed record DestinationValidationResult(bool IsPermitted, DestinationRefusal Refusal)
{
    public static DestinationValidationResult Permitted() => new(true, DestinationRefusal.None);

    public static DestinationValidationResult Refused(DestinationRefusal refusal) =>
        new(false, refusal);
}

/// <summary>
/// Orchestrates a destination check: resolve, then hand every fact to the Domain predicate.
///
/// It decides nothing. <c>layers.md §1.3</c> — "It contains sequencing and coordination,
/// not business rules." What a failed resolution means is a rule about a permitted
/// destination, so this passes the outcome through untouched and lets Domain judge it.
/// </summary>
public sealed class ValidateDestination(IHostResolver resolver)
{
    public async Task<DestinationValidationResult> ExecuteAsync(
        string? rawUrl,
        CancellationToken cancellationToken)
    {
        // The scheme is checked first so a caller supplying "javascript:alert(1)" does not
        // cause a network call on our behalf.
        var schemeVerdict = DestinationPolicy.CheckScheme(rawUrl);

        if (!schemeVerdict.IsPermitted)
        {
            return Translate(schemeVerdict.Rejection);
        }

        var host = new Uri(rawUrl!).Host;
        var resolution = await resolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);

        // The outcome goes straight to Domain. No branch on it here — that is the flag
        // architecture-advisor raised against an earlier shape of this method.
        return Translate(DestinationPolicy.CheckFully(rawUrl, resolution).Rejection);
    }

    private static DestinationValidationResult Translate(DestinationRejection rejection) =>
        rejection == DestinationRejection.None
            ? DestinationValidationResult.Permitted()
            : DestinationValidationResult.Refused(rejection switch
            {
                DestinationRejection.NotAbsoluteUrl => DestinationRefusal.NotAbsoluteUrl,
                DestinationRejection.SchemeNotPermitted => DestinationRefusal.SchemeNotPermitted,
                DestinationRejection.HostNotResolved => DestinationRefusal.HostNotResolved,
                DestinationRejection.ResolutionFailed => DestinationRefusal.ResolutionFailed,
                DestinationRejection.AddressNotPermitted => DestinationRefusal.AddressNotPermitted,
                _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, null)
            });
}
