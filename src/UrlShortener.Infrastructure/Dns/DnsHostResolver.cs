using System.Net;
using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Infrastructure.Dns;

/// <summary>
/// Resolves a hostname to addresses. Built to ADR-001.
///
/// Returns facts and never judgments — no filtering, no ranking, no truncation.
/// <c>layers.md §1.4</c>: "It contains no business rule and no use-case sequencing."
/// Deciding that a loopback address is not a permitted destination happens in Domain.
/// </summary>
public sealed class DnsHostResolver : IHostResolver
{
    private readonly TimeSpan _timeout;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _lookup;

    /// <param name="timeout">
    /// Explicit, per <c>STD-OPS-06</c>. ADR-001 sets the default at two seconds so a
    /// resolver outage cannot sit against the create path's 200 ms p99 budget for long.
    /// </param>
    /// <param name="lookup">
    /// The lookup itself, injectable so the timeout and failure paths are testable without
    /// a network. Defaults to system DNS.
    /// </param>
    public DnsHostResolver(
        TimeSpan timeout,
        Func<string, CancellationToken, Task<IPAddress[]>>? lookup = null)
    {
        _timeout = timeout;
        _lookup = lookup ?? ((host, ct) => System.Net.Dns.GetHostAddressesAsync(host, ct));
    }

    public async Task<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        using var timeoutSource = new CancellationTokenSource(_timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutSource.Token);

        try
        {
            var addresses = await _lookup(host, linked.Token).ConfigureAwait(false);

            return addresses.Length == 0
                ? new HostResolution.NotFound()
                : new HostResolution.Resolved(addresses);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The caller gave up. That is not a policy answer, so it propagates rather
            // than being reported as a destination that could not be resolved.
            throw;
        }
        catch (OperationCanceledException)
        {
            // Our own timeout expired.
            return new HostResolution.Failed();
        }
        catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException)
        {
            // §6.3 — a lower-level exception is translated into a failure type this
            // interface declares, rather than escaping into the caller unchanged.
            return new HostResolution.Failed();
        }
    }
}
