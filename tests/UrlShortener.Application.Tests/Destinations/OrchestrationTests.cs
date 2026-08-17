using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Tests.Destinations;

/// <summary>
/// T-16 — the orchestration decides nothing.
///
/// architecture-advisor flagged this: "any accept/reject decision the orchestration makes
/// on its own — DNS resolution failed, no A record returned … — is a rule about a
/// permitted destination, and §1.3 says the Application layer contains not business
/// rules." The tests below assert the orchestration still calls the Domain on every
/// outcome rather than short-circuiting.
/// </summary>
public class OrchestrationTests
{
    private sealed class FakeResolver(HostResolution outcome) : IHostResolver
    {
        public int Calls { get; private set; }
        public string? LastHost { get; private set; }

        public Task<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken)
        {
            Calls++;
            LastHost = host;
            return Task.FromResult(outcome);
        }
    }

    [Fact]
    public async Task A_permitted_destination_is_permitted()
    {
        var resolver = new FakeResolver(new HostResolution.Resolved(
            [System.Net.IPAddress.Parse("93.184.216.34")]));

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync("https://example.com", CancellationToken.None);

        Assert.True(result.IsPermitted);
        Assert.Equal(DestinationRefusal.None, result.Refusal);
    }

    // T-16 — on NotFound it still asks the Domain; it does not decide itself
    [Fact]
    public async Task An_unresolvable_host_is_refused_with_the_domain_reason()
    {
        var resolver = new FakeResolver(new HostResolution.NotFound());

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync("https://nowhere.example", CancellationToken.None);

        Assert.False(result.IsPermitted);
        Assert.Equal(DestinationRefusal.HostNotResolved, result.Refusal);
        Assert.Equal(1, resolver.Calls);
    }

    // T-16 — on Failed it still asks the Domain
    [Fact]
    public async Task A_failed_resolution_is_refused_with_the_domain_reason()
    {
        var resolver = new FakeResolver(new HostResolution.Failed());

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync("https://timeout.example", CancellationToken.None);

        Assert.False(result.IsPermitted);
        Assert.Equal(DestinationRefusal.ResolutionFailed, result.Refusal);
        Assert.Equal(1, resolver.Calls);
    }

    /// <summary>
    /// A destination refused on its scheme never reaches the resolver. Not a layering
    /// rule — a caller supplying "javascript:alert(1)" should not cause a network call.
    /// </summary>
    [Fact]
    public async Task A_bad_scheme_is_refused_without_resolving_anything()
    {
        var resolver = new FakeResolver(new HostResolution.Failed());

        var result = await new ValidateDestination(resolver)
            .ExecuteAsync("javascript:alert(1)", CancellationToken.None);

        Assert.Equal(DestinationRefusal.SchemeNotPermitted, result.Refusal);
        Assert.Equal(0, resolver.Calls);
    }

    [Fact]
    public async Task The_host_passed_to_the_resolver_is_the_url_host()
    {
        var resolver = new FakeResolver(new HostResolution.Resolved(
            [System.Net.IPAddress.Parse("93.184.216.34")]));

        await new ValidateDestination(resolver)
            .ExecuteAsync("https://sub.example.com:8443/path?q=1", CancellationToken.None);

        Assert.Equal("sub.example.com", resolver.LastHost);
    }

    /// <summary>
    /// The Application result type is its own, not the Domain's. layers.md §5.2 — a use
    /// case returns types it declares, so a Domain rename cannot reach the wire contract.
    /// </summary>
    [Fact]
    public void The_application_result_does_not_expose_domain_types()
    {
        var refusalProperty = typeof(DestinationValidationResult).GetProperty(
            nameof(DestinationValidationResult.Refusal))!;

        Assert.Equal(typeof(DestinationRefusal), refusalProperty.PropertyType);
        Assert.NotEqual(typeof(DestinationRejection), refusalProperty.PropertyType);
    }
}
