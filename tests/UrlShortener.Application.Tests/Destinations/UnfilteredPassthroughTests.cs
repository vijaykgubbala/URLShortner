using System.Net;
using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Application.Tests.Destinations;

/// <summary>
/// T-15 — every resolved address reaches the predicate.
///
/// If the orchestration filtered, ranked, or took the first address, a private address
/// hidden among public ones would never be judged. These assert the refusal happens
/// regardless of where in the answer the disallowed address sits.
/// </summary>
public class UnfilteredPassthroughTests
{
    private sealed class StubResolver(params string[] addresses) : IHostResolver
    {
        public Task<HostResolution> ResolveAsync(string host, CancellationToken cancellationToken) =>
            Task.FromResult<HostResolution>(
                new HostResolution.Resolved(addresses.Select(IPAddress.Parse).ToArray()));
    }

    [Theory]
    [InlineData("10.0.0.5", "93.184.216.34", "1.1.1.1", "8.8.8.8")]   // first
    [InlineData("93.184.216.34", "10.0.0.5", "1.1.1.1", "8.8.8.8")]   // middle
    [InlineData("93.184.216.34", "1.1.1.1", "8.8.8.8", "10.0.0.5")]   // last
    public async Task A_disallowed_address_anywhere_in_the_answer_refuses_the_destination(
        params string[] addresses)
    {
        var result = await new ValidateDestination(new StubResolver(addresses))
            .ExecuteAsync("https://example.com", CancellationToken.None);

        Assert.False(result.IsPermitted);
        Assert.Equal(DestinationRefusal.AddressNotPermitted, result.Refusal);
    }

    [Fact]
    public async Task An_answer_of_only_public_addresses_is_permitted()
    {
        var result = await new ValidateDestination(
                new StubResolver("93.184.216.34", "1.1.1.1", "8.8.8.8"))
            .ExecuteAsync("https://example.com", CancellationToken.None);

        Assert.True(result.IsPermitted);
    }
}
