namespace UrlShortener.Domain.Destinations;

/// <summary>Why a destination was refused. <see cref="None"/> means permitted.</summary>
public enum DestinationRejection
{
    None = 0,
    NotAbsoluteUrl,
    SchemeNotPermitted,
    HostNotResolved,
    ResolutionFailed,
    AddressNotPermitted
}

/// <summary>
/// The result of checking the scheme alone. Deliberately a separate type from
/// <see cref="FullVerdict"/> so a caller cannot pass a partially checked destination
/// where a fully checked one is required — that mistake fails open.
/// </summary>
public sealed record SchemeVerdict(DestinationRejection Rejection)
{
    public bool IsPermitted => Rejection == DestinationRejection.None;
}

/// <summary>The result of checking scheme and every resolved address.</summary>
public sealed record FullVerdict(DestinationRejection Rejection)
{
    public bool IsPermitted => Rejection == DestinationRejection.None;
}

/// <summary>
/// Decides whether a destination URL may be stored and redirected to.
/// Pure and synchronous: it judges facts it is handed and performs no I/O.
/// </summary>
public static class DestinationPolicy
{
    /// <summary>
    /// Checks the scheme alone. Used on the redirect path, which cannot afford a DNS
    /// lookup against its 50 ms p99 budget.
    /// </summary>
    public static SchemeVerdict CheckScheme(string? rawUrl) => new(RejectionFor(rawUrl, out _));

    /// <summary>
    /// Checks the scheme and every resolved address. Used at creation, where the cost of
    /// resolution is paid once.
    /// </summary>
    public static FullVerdict CheckFully(string? rawUrl, HostResolution resolution)
    {
        var schemeRejection = RejectionFor(rawUrl, out _);

        return schemeRejection != DestinationRejection.None
            ? new FullVerdict(schemeRejection)
            : new FullVerdict(DestinationRejection.None);
    }

    /// <summary>
    /// Order matters. A scheme is judged before a host is required, because
    /// "javascript:alert(1)" is a well-formed absolute URI with no host: refusing it as
    /// unparseable would report a 400 where the policy means 422.
    /// </summary>
    private static DestinationRejection RejectionFor(string? rawUrl, out Uri? uri)
    {
        uri = null;

        if (string.IsNullOrWhiteSpace(rawUrl) || !Uri.TryCreate(rawUrl, UriKind.Absolute, out uri))
        {
            return DestinationRejection.NotAbsoluteUrl;
        }

        if (!IsPermittedScheme(uri.Scheme))
        {
            return DestinationRejection.SchemeNotPermitted;
        }

        return string.IsNullOrEmpty(uri.Host)
            ? DestinationRejection.NotAbsoluteUrl
            : DestinationRejection.None;
    }

    private static bool IsPermittedScheme(string scheme) =>
        scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
        || scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
}
