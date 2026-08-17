using System.Net;
using System.Net.Sockets;

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

        if (schemeRejection != DestinationRejection.None)
        {
            return new FullVerdict(schemeRejection);
        }

        // Every arm refuses except a resolution that produced addresses and passed them.
        // This is where "fail closed" is decided, and it is decided in Domain because what
        // an unresolvable host means is a rule about a permitted destination.
        return resolution switch
        {
            HostResolution.Resolved { Addresses.Count: 0 } =>
                new FullVerdict(DestinationRejection.HostNotResolved),

            HostResolution.Resolved resolved =>
                new FullVerdict(JudgeAddresses(resolved.Addresses)),

            HostResolution.NotFound => new FullVerdict(DestinationRejection.HostNotResolved),

            HostResolution.Failed => new FullVerdict(DestinationRejection.ResolutionFailed),

            _ => new FullVerdict(DestinationRejection.ResolutionFailed)
        };
    }

    private static DestinationRejection JudgeAddresses(IReadOnlyList<IPAddress> addresses) =>
        // Any single disallowed address refuses the destination. Our resolver and the
        // visitor's may pick differently from the same set, so requiring all of them to be
        // disallowed would fail open on exactly the case that matters.
        addresses.All(IsPermittedAddress)
            ? DestinationRejection.None
            : DestinationRejection.AddressNotPermitted;

    private static bool IsPermittedAddress(IPAddress address)
    {
        // Judge a mapped address on what it maps to. Otherwise ::ffff:127.0.0.1 reaches
        // loopback on any dual-stack host, and the control is bypassed by writing the
        // same address a different way.
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return address.AddressFamily == AddressFamily.InterNetwork
            ? IsPermittedV4(address)
            : IsPermittedV6(address);
    }

    private static bool IsPermittedV4(IPAddress address)
    {
        var b = address.GetAddressBytes();

        return b[0] switch
        {
            0 => false,                                   // 0.0.0.0/8  unspecified
            10 => false,                                  // 10.0.0.0/8 private
            127 => false,                                 // 127.0.0.0/8 loopback
            100 when b[1] is >= 64 and <= 127 => false,   // 100.64.0.0/10 carrier-grade NAT
            169 when b[1] == 254 => false,                // 169.254.0.0/16 link-local
            172 when b[1] is >= 16 and <= 31 => false,    // 172.16.0.0/12 private
            192 when b[1] == 168 => false,                // 192.168.0.0/16 private
            255 => false,                                 // 255.0.0.0/8 reserved, broadcast
            _ => true
        };
    }

    private static bool IsPermittedV6(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.IPv6Any))
        {
            return false;                                 // ::1 and ::
        }

        if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal)
        {
            return false;                                 // fe80::/10 and the deprecated fec0::/10
        }

        if (address.IsIPv6Multicast)
        {
            return false;                                 // ff00::/8, incl. SSDP over IPv6
        }

        var b = address.GetAddressBytes();

        if ((b[0] & 0xFE) == 0xFC)
        {
            return false;                                 // fc00::/7 unique-local
        }

        // Any encoding that embeds an IPv4 address is judged on the address it embeds.
        // The mapped ::ffff: form was already handled; it is one spelling out of four, and
        // handling one is what let ::10.0.0.1, NAT64 and 6to4 through.
        if (TryExtractEmbeddedV4(b, out var embedded))
        {
            return IsPermittedV4(embedded);
        }

        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8)
        {
            return false;                                 // 2001:db8::/32 documentation
        }

        return !(b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x00 && b[3] == 0x00); // 2001::/32 Teredo
    }

    /// <summary>
    /// Recovers the IPv4 address carried inside an IPv6 encoding. Each of these reaches an
    /// IPv4 destination while looking like an IPv6 one, so each must be judged by the IPv4
    /// rules rather than the IPv6 ones.
    /// </summary>
    private static bool TryExtractEmbeddedV4(byte[] b, out IPAddress embedded)
    {
        embedded = IPAddress.None;

        // ::a.b.c.d — IPv4-compatible, high 96 bits zero. ::1 and :: are already refused.
        if (b.Take(12).All(octet => octet == 0))
        {
            embedded = new IPAddress(b[12..16]);
            return true;
        }

        // 64:ff9b::/96 — the NAT64 well-known prefix.
        if (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xFF && b[3] == 0x9B)
        {
            embedded = new IPAddress(b[12..16]);
            return true;
        }

        // 2002:V4::/16 — 6to4 carries the address in the second through fifth octets.
        if (b[0] == 0x20 && b[1] == 0x02)
        {
            embedded = new IPAddress(b[2..6]);
            return true;
        }

        return false;
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
