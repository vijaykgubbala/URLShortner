using Microsoft.Extensions.Logging;
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
    AddressNotPermitted,
    UserInfoNotPermitted
}

/// <summary>
/// The Application-declared result of validating a destination.
///
/// Exists so Entrypoints never maps a Domain type onto the published HTTP contract
/// (<c>layers.md §5.2</c>, <c>§5.3</c>). Without it, renaming a Domain enum member would
/// silently change a wire contract that <c>api.md §4.2</c> says must not change once
/// published.
/// </summary>
public sealed record DestinationValidationResult(
    bool IsPermitted,
    DestinationRefusal Refusal,
    string? NormalisedUrl = null)
{
    /// <summary>
    /// Carries the normalised destination the Domain actually judged. Callers persist
    /// this, never the caller's raw string — otherwise the value validated and the value
    /// later emitted in a Location header are different strings.
    /// </summary>
    public static DestinationValidationResult Permitted(string? normalisedUrl) =>
        new(true, DestinationRefusal.None, normalisedUrl);

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
public sealed class ValidateDestination(
    IHostResolver resolver,
    ILogger<ValidateDestination>? logger = null,
    RejectionCounter? counter = null)
{
    private static readonly EventId Rejected = new(1000, "link.destination.rejected");

    /// <summary>
    /// The length bound <c>STD-SEC-02</c> requires. The rule asks for type, range, length
    /// and format at the trust boundary; only type and format existed, so an unbounded
    /// string reached DNS resolution and the log. 2048 is the conventional URL cap.
    /// </summary>
    public const int MaxDestinationLength = 2048;

    public async Task<DestinationValidationResult> ExecuteAsync(
        string? rawUrl,
        CancellationToken cancellationToken)
    {
        // Length is checked before anything else — before parsing, before resolution,
        // before logging — so an oversized value never reaches a downstream cost.
        if (rawUrl is { Length: > MaxDestinationLength })
        {
            return Record(
                DestinationValidationResult.Refused(DestinationRefusal.NotAbsoluteUrl),
                host: null,
                resolution: null);
        }

        // The scheme is checked next so a caller supplying "javascript:alert(1)" does not
        // cause a network call on our behalf.
        var schemeVerdict = DestinationPolicy.CheckScheme(rawUrl);

        if (!schemeVerdict.IsPermitted)
        {
            return Record(Translate(schemeVerdict.Rejection, null), host: null, resolution: null);
        }

        var host = new Uri(rawUrl!).Host;
        var resolution = await resolver.ResolveAsync(host, cancellationToken).ConfigureAwait(false);

        // The outcome goes straight to Domain. No branch on it here — that is the flag
        // architecture-advisor raised against an earlier shape of this method.
        var verdict = DestinationPolicy.CheckFully(rawUrl, resolution);

        return Record(Translate(verdict.Rejection, verdict.NormalisedUrl), host, resolution);
    }

    /// <summary>
    /// Emits one event and one increment per refusal — not one per rule evaluated. Three
    /// disallowed addresses in one answer is one refused destination, and counting it
    /// three times would make a single probe look like three.
    ///
    /// The resolved addresses are logged here and nowhere else. <c>api.md §4.3</c> keeps
    /// them out of the response body; this is the only place an operator can see which
    /// address caused the refusal, correlated by the trace identifier on the response.
    /// </summary>
    private DestinationValidationResult Record(
        DestinationValidationResult result,
        string? host,
        HostResolution? resolution)
    {
        if (result.IsPermitted)
        {
            return result;
        }

        counter?.Increment(result.Refusal);

        var addresses = resolution is HostResolution.Resolved resolved
            ? string.Join(", ", resolved.Addresses)
            : "(not resolved)";

        // The host and the resolved addresses, never the submitted URL. STD-SEC-04 forbids
        // a full request payload as a log argument, and CreateShortLinkRequest has exactly
        // one member — so logging the destination logs the whole body of an unauthenticated
        // POST, including whatever the submitter put in its query string.
        logger?.LogWarning(
            Rejected,
            "Destination refused. Reason: {Reason}. Host: {Host}. Addresses: {Addresses}.",
            result.Refusal,
            host ?? "(not parsed)",
            addresses);

        return result;
    }

    private static DestinationValidationResult Translate(
        DestinationRejection rejection,
        string? normalisedUrl) =>
        rejection == DestinationRejection.None
            ? DestinationValidationResult.Permitted(normalisedUrl)
            : DestinationValidationResult.Refused(rejection switch
            {
                DestinationRejection.NotAbsoluteUrl => DestinationRefusal.NotAbsoluteUrl,
                DestinationRejection.SchemeNotPermitted => DestinationRefusal.SchemeNotPermitted,
                DestinationRejection.HostNotResolved => DestinationRefusal.HostNotResolved,
                DestinationRejection.ResolutionFailed => DestinationRefusal.ResolutionFailed,
                DestinationRejection.AddressNotPermitted => DestinationRefusal.AddressNotPermitted,
                DestinationRejection.UserInfoNotPermitted => DestinationRefusal.UserInfoNotPermitted,
                _ => throw new ArgumentOutOfRangeException(nameof(rejection), rejection, null)
            });
}
