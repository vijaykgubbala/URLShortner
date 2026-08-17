using System.Net;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Domain.Tests.Destinations;

/// <summary>T-04 … T-09 — addresses that must never be redirected to.</summary>
public class AddressRangeTests
{
    private static FullVerdict Check(params string[] addresses) =>
        DestinationPolicy.CheckFully(
            "https://example.com",
            new HostResolution.Resolved(addresses.Select(IPAddress.Parse).ToArray()));

    // T-04 — IPv4 loopback
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.0.0.53")]
    [InlineData("127.255.255.254")]
    public void Ipv4_loopback_is_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    // T-05 — IPv4 private ranges, including their boundaries
    [Theory]
    [InlineData("10.0.0.0")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.0")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.0.1")]
    public void Ipv4_private_ranges_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    // T-05 boundary — addresses just outside 172.16.0.0/12 are public
    [Theory]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.0")]
    [InlineData("11.0.0.1")]
    [InlineData("192.167.255.255")]
    public void Addresses_outside_the_private_ranges_are_permitted(string address)
    {
        Assert.Equal(DestinationRejection.None, Check(address).Rejection);
    }

    // T-06 — link-local, unspecified, broadcast, CGNAT
    [Theory]
    [InlineData("169.254.1.1")]
    [InlineData("0.0.0.0")]
    [InlineData("255.255.255.255")]
    [InlineData("100.64.0.1")]
    public void Ipv4_link_local_and_reserved_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    /// <summary>
    /// GATE-17 F-1. AC-3 says "loopback, link-local, private or <b>reserved</b>". The
    /// switch implemented four named examples of "reserved" and left the category open,
    /// so these were permitted while 119 tests agreed with the code — because no test
    /// asserted the criterion. The IPv6 equivalent was fixed a commit earlier; this is
    /// the half that fell between the handover that named it and a review told to skip it.
    /// </summary>
    [Theory]
    [InlineData("224.0.0.1")]        // all-systems multicast
    [InlineData("239.255.255.250")]  // SSDP — local network discovery
    [InlineData("232.1.1.1")]        // source-specific multicast
    [InlineData("240.0.0.1")]        // reserved for future use
    [InlineData("254.1.2.3")]        // reserved 240/4, below the broadcast address
    [InlineData("198.18.0.1")]       // benchmark 198.18/15
    [InlineData("198.19.255.255")]   // benchmark, upper bound
    [InlineData("192.0.0.1")]        // IETF protocol assignments
    [InlineData("192.0.2.1")]        // TEST-NET-1
    [InlineData("198.51.100.1")]     // TEST-NET-2
    [InlineData("203.0.113.1")]      // TEST-NET-3
    public void Ipv4_reserved_ranges_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    /// <summary>
    /// The counterpart. Each of these sits immediately outside a range refused above, so
    /// a mask written one bit too wide fails here while still passing every refusal row.
    /// </summary>
    [Theory]
    [InlineData("223.255.255.255")]  // just below multicast 224/4
    [InlineData("198.17.255.255")]   // just below benchmark 198.18/15
    [InlineData("198.20.0.1")]       // just above benchmark 198.18/15
    [InlineData("192.0.1.1")]        // between 192.0.0/24 and TEST-NET-1
    [InlineData("192.0.3.1")]        // just above TEST-NET-1
    [InlineData("198.51.101.1")]     // just above TEST-NET-2
    [InlineData("203.0.114.1")]      // just above TEST-NET-3
    [InlineData("93.184.216.34")]    // ordinary public address
    public void Ipv4_addresses_adjacent_to_the_reserved_ranges_are_permitted(string address)
    {
        Assert.Equal(DestinationRejection.None, Check(address).Rejection);
    }

    // T-07 — IPv6 loopback, link-local, site-local, unique-local
    [Theory]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("::")]
    [InlineData("fec0::1")]  // TST-007 — the deprecated site-local arm had no row
    public void Ipv6_loopback_link_local_site_local_and_unique_local_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    /// <summary>
    /// COR-002 / SEC-002 / TST-002 — AC-3's "reserved" clause on the IPv6 side.
    /// Three review lenses reached this independently.
    /// </summary>
    [Theory]
    [InlineData("ff02::1")]      // all-nodes multicast
    [InlineData("ff05::c")]      // SSDP over IPv6
    [InlineData("ff0e::1")]      // global-scope multicast
    [InlineData("2001:db8::1")]  // documentation
    [InlineData("2001::1")]      // Teredo
    public void Ipv6_reserved_ranges_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    /// <summary>
    /// COR-002 / SEC-002 — every encoding that embeds an IPv4 address is judged on the
    /// address it embeds, not only the ::ffff: form. Each of these reaches a private or
    /// loopback address by writing it a different way, which is the bypass the mapped-form
    /// guard already existed to close — it was closed for one spelling out of four.
    /// </summary>
    [Theory]
    [InlineData("::a00:1")]            // ::10.0.0.1 IPv4-compatible
    [InlineData("::7f00:1")]           // ::127.0.0.1 IPv4-compatible
    [InlineData("64:ff9b::a00:1")]     // NAT64 of 10.0.0.1
    [InlineData("64:ff9b::7f00:1")]    // NAT64 of 127.0.0.1
    [InlineData("2002:a00:1::")]       // 6to4 of 10.0.0.1
    [InlineData("2002:7f00:1::")]      // 6to4 of 127.0.0.1
    public void Ipv6_encodings_embedding_a_private_ipv4_address_are_refused(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    /// <summary>
    /// The counterpart. Without this, refusing every IPv6 address would satisfy the rows above.
    /// </summary>
    [Theory]
    [InlineData("64:ff9b::5db8:d822")]  // NAT64 of a public address
    [InlineData("2002:5db8:d822::")]    // 6to4 of a public address
    public void Ipv6_encodings_embedding_a_public_ipv4_address_are_permitted(string address)
    {
        Assert.Equal(DestinationRejection.None, Check(address).Rejection);
    }

    /// <summary>
    /// T-08 — an IPv4-mapped IPv6 address is judged on the address it maps to.
    /// Without this, ::ffff:127.0.0.1 reaches loopback on any dual-stack host and the
    /// whole control is bypassed by writing the address a different way.
    /// </summary>
    [Theory]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:192.168.1.1")]
    public void Ipv4_mapped_ipv6_is_judged_on_its_mapped_value(string address)
    {
        Assert.Equal(DestinationRejection.AddressNotPermitted, Check(address).Rejection);
    }

    [Fact]
    public void A_public_ipv6_address_is_permitted()
    {
        Assert.Equal(DestinationRejection.None, Check("2606:4700:4700::1111").Rejection);
    }

    // T-09 — any disallowed address in the set rejects the whole destination
    [Fact]
    public void One_disallowed_address_among_several_refuses_the_destination()
    {
        var verdict = Check("93.184.216.34", "1.1.1.1", "10.0.0.5", "8.8.8.8");

        Assert.Equal(DestinationRejection.AddressNotPermitted, verdict.Rejection);
    }

    [Fact]
    public void All_public_addresses_are_permitted()
    {
        Assert.Equal(DestinationRejection.None, Check("93.184.216.34", "1.1.1.1", "8.8.8.8").Rejection);
    }
}
