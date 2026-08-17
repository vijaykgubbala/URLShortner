using UrlShortener.Domain.Destinations;

namespace UrlShortener.Domain.Tests.Destinations;

/// <summary>T-13, T-14 — a value that is not an absolute URL is refused as unparseable.</summary>
public class ParseFailureTests
{
    // T-13
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    [InlineData("http://")]
    [InlineData("http://exa mple.com")]
    [InlineData("::::")]
    public void Unparseable_values_are_refused_as_not_an_absolute_url(string? raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.NotAbsoluteUrl, verdict.Rejection);
    }

    // T-14
    [Theory]
    [InlineData("/path/only")]
    [InlineData("example.com")]
    [InlineData("example.com/path")]
    public void Relative_urls_are_refused_rather_than_accepted(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.NotAbsoluteUrl, verdict.Rejection);
    }

    /// <summary>
    /// T-14 boundary. A scheme-relative reference is NOT a parse failure: on Windows
    /// "//host" parses as an absolute file:// URI. It is therefore refused by the scheme
    /// rule rather than the parse rule — a 422, not a 400. Pinned here because the
    /// behaviour is platform-dependent and the distinction decides the status code.
    /// </summary>
    [Fact]
    public void Scheme_relative_reference_parses_as_absolute_and_is_not_a_parse_failure()
    {
        var verdict = DestinationPolicy.CheckScheme("//scheme-relative.example.com");

        Assert.NotEqual(DestinationRejection.NotAbsoluteUrl, verdict.Rejection);
    }
}
