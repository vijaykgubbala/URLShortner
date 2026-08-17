using UrlShortener.Application.Destinations;

namespace UrlShortener.Api.Destinations;

/// <summary>One invalid field and why, per <c>api.md §4.4</c>.</summary>
public sealed record FieldError(string Field, string Reason);

/// <summary>
/// The single error body shape from <c>api.md §4.1</c>.
///
/// Built from the Application refusal, never from a Domain type — <c>layers.md §5.2</c>
/// and <c>§5.3</c>. The published <see cref="Type"/> strings are chosen here and are the
/// contract; <c>api.md §4.2</c> says they must not change once published, which is why
/// they are written out rather than derived from an enum name.
/// </summary>
public sealed record DestinationProblem(
    string Type,
    string Title,
    int Status,
    string Detail,
    string TraceId,
    IReadOnlyList<FieldError> Errors)
{
    private const string DestinationField = "destination";

    public static DestinationProblem? FromOrNull(DestinationRefusal refusal, string traceId) =>
        refusal == DestinationRefusal.None ? null : From(refusal, traceId);

    public static DestinationProblem From(DestinationRefusal refusal, string traceId) => refusal switch
    {
        DestinationRefusal.NotAbsoluteUrl => Invalid(
            [(DestinationField, "must be an absolute http or https URL")], traceId),

        DestinationRefusal.SchemeNotPermitted => Policy(
            "destination-scheme-not-permitted",
            "The destination URL uses a scheme that is not permitted.",
            "Only http and https destinations can be shortened.",
            traceId),

        DestinationRefusal.AddressNotPermitted => Policy(
            "destination-address-not-permitted",
            "The destination URL is not a permitted destination.",
            // Deliberately says nothing about which address, or that an address was
            // involved at all — api.md §4.3. The address is in the log, under this traceId.
            "This destination cannot be shortened.",
            traceId),

        DestinationRefusal.HostNotResolved => Policy(
            "destination-host-not-resolved",
            "The destination host could not be found.",
            "The host in the destination URL did not resolve.",
            traceId),

        DestinationRefusal.ResolutionFailed => Policy(
            "destination-resolution-failed",
            "The destination could not be checked.",
            "The destination host could not be checked. Try again shortly.",
            traceId),

        _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, null)
    };

    /// <summary>
    /// A 400 carrying every invalid field at once — <c>api.md §4.4</c>: "Validation
    /// failures are reported together, not one at a time."
    /// </summary>
    public static DestinationProblem Invalid(
        IReadOnlyList<(string Field, string Reason)> failures,
        string traceId) =>
        new(
            "invalid-request",
            "The request could not be understood.",
            400,
            "One or more fields are invalid.",
            traceId,
            failures.Select(f => new FieldError(f.Field, f.Reason)).ToArray());

    private static DestinationProblem Policy(
        string type, string title, string detail, string traceId) =>
        new(type, title, 422, detail, traceId, []);
}
