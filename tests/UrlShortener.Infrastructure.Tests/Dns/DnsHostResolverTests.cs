using System.Net;
using UrlShortener.Domain.Destinations;
using UrlShortener.Infrastructure.Dns;

namespace UrlShortener.Infrastructure.Tests.Dns;

/// <summary>
/// T-17, T-18 — the adapter returns facts and never judgments.
///
/// architecture-advisor flagged this: "the adapter is the natural place for someone to
/// filter or normalise addresses (drop loopback, drop IPv6, pick the first result).
/// §1.4 says Infrastructure contains no business rule."
/// </summary>
public class DnsHostResolverTests
{
    private static readonly TimeSpan Generous = TimeSpan.FromSeconds(5);

    // T-17
    [Fact]
    public async Task Every_resolved_address_is_returned_unfiltered()
    {
        IPAddress[] answer =
        [
            IPAddress.Parse("127.0.0.1"),
            IPAddress.Parse("93.184.216.34"),
            IPAddress.Parse("10.0.0.1"),
            IPAddress.Parse("2606:4700::1111")
        ];
        var resolver = new DnsHostResolver(Generous, (_, _) => Task.FromResult(answer));

        var outcome = await resolver.ResolveAsync("example.com", CancellationToken.None);

        var resolved = Assert.IsType<HostResolution.Resolved>(outcome);
        Assert.Equal(4, resolved.Addresses.Count);
        Assert.Contains(IPAddress.Parse("127.0.0.1"), resolved.Addresses);
        Assert.Contains(IPAddress.Parse("10.0.0.1"), resolved.Addresses);
    }

    // T-17 — unranked: the order the resolver gave is the order returned
    [Fact]
    public async Task The_order_returned_by_the_resolver_is_preserved()
    {
        IPAddress[] answer =
        [
            IPAddress.Parse("2606:4700::1111"),
            IPAddress.Parse("93.184.216.34")
        ];
        var resolver = new DnsHostResolver(Generous, (_, _) => Task.FromResult(answer));

        var resolved = Assert.IsType<HostResolution.Resolved>(
            await resolver.ResolveAsync("example.com", CancellationToken.None));

        Assert.Equal(answer, resolved.Addresses);
    }

    [Fact]
    public async Task A_host_with_no_addresses_is_NotFound()
    {
        var resolver = new DnsHostResolver(Generous, (_, _) => Task.FromResult(Array.Empty<IPAddress>()));

        Assert.IsType<HostResolution.NotFound>(
            await resolver.ResolveAsync("nowhere.example", CancellationToken.None));
    }

    // T-18 — an explicit timeout, per STD-OPS-06
    [Fact]
    public async Task A_lookup_that_exceeds_the_timeout_returns_Failed()
    {
        var resolver = new DnsHostResolver(
            TimeSpan.FromMilliseconds(50),
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return [IPAddress.Parse("93.184.216.34")];
            });

        var outcome = await resolver.ResolveAsync("slow.example", CancellationToken.None);

        Assert.IsType<HostResolution.Failed>(outcome);
    }

    /// <summary>
    /// A resolver error is a returned value, not a thrown exception. ADR-001: a thrown
    /// failure would force the caller into a catch block that decides whether to reject,
    /// and that decision is a business rule the layer model keeps in the domain.
    /// </summary>
    [Fact]
    public async Task A_resolver_error_is_returned_as_Failed_rather_than_thrown()
    {
        var resolver = new DnsHostResolver(
            Generous,
            (_, _) => throw new System.Net.Sockets.SocketException(11001));

        var outcome = await resolver.ResolveAsync("broken.example", CancellationToken.None);

        Assert.IsType<HostResolution.Failed>(outcome);
    }

    /// <summary>
    /// Cancellation by the caller is distinct from the adapter's own timeout: it propagates
    /// rather than being swallowed as a policy answer. A caller who gave up has not asked
    /// a question that needs answering.
    /// </summary>
    [Fact]
    public async Task Caller_cancellation_propagates_rather_than_becoming_Failed()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var resolver = new DnsHostResolver(
            Generous,
            async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return [IPAddress.Parse("93.184.216.34")];
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => resolver.ResolveAsync("example.com", cts.Token));
    }
}
