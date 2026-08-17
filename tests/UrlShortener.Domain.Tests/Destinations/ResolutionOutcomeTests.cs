using UrlShortener.Domain.Destinations;

namespace UrlShortener.Domain.Tests.Destinations;

/// <summary>
/// T-10, T-11 — the Domain decides what a failed resolution means, and it fails closed.
///
/// This lives in Domain rather than the orchestration because deciding that an
/// unresolvable host is not a permitted destination is a rule about a permitted
/// destination — <c>layers.md §1.3</c> keeps business rules out of the Application layer.
/// </summary>
public class ResolutionOutcomeTests
{
    private const string ValidUrl = "https://example.com";

    // T-10
    [Fact]
    public void A_host_that_does_not_resolve_is_refused()
    {
        var verdict = DestinationPolicy.CheckFully(ValidUrl, new HostResolution.NotFound());

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.HostNotResolved, verdict.Rejection);
    }

    // T-11 — fails closed
    [Fact]
    public void A_resolution_that_fails_is_refused_rather_than_permitted()
    {
        var verdict = DestinationPolicy.CheckFully(ValidUrl, new HostResolution.Failed());

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.ResolutionFailed, verdict.Rejection);
    }

    [Fact]
    public void A_host_resolving_to_no_addresses_at_all_is_refused()
    {
        var verdict = DestinationPolicy.CheckFully(
            ValidUrl,
            new HostResolution.Resolved(Array.Empty<System.Net.IPAddress>()));

        Assert.False(verdict.IsPermitted);
    }

    /// <summary>
    /// A bad scheme is reported as a scheme problem even when resolution also failed.
    /// Reporting the resolution failure instead would tell a caller to retry a request
    /// that will never succeed.
    /// </summary>
    [Fact]
    public void A_scheme_rejection_is_reported_ahead_of_a_resolution_failure()
    {
        var verdict = DestinationPolicy.CheckFully("ftp://example.com", new HostResolution.Failed());

        Assert.Equal(DestinationRejection.SchemeNotPermitted, verdict.Rejection);
    }
}
