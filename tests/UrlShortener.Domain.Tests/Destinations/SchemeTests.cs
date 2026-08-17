using UrlShortener.Domain.Destinations;

namespace UrlShortener.Domain.Tests.Destinations;

/// <summary>T-01, T-02, T-03 — only http and https are permitted schemes.</summary>
public class SchemeTests
{
    // T-01
    [Theory]
    [InlineData("http://example.com")]
    [InlineData("HTTP://EXAMPLE.COM")]
    [InlineData("http://example.com./path")]
    public void Http_is_permitted(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.True(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.None, verdict.Rejection);
    }

    // T-02
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("https://example.com:8443/path?q=1")]
    [InlineData("https://user:pw@example.com/path")]
    public void Https_is_permitted(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.True(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.None, verdict.Rejection);
    }

    // T-03
    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://example.com/f")]
    [InlineData("mailto:someone@example.com")]
    [InlineData("//scheme-relative.example.com")]
    public void Every_other_scheme_is_refused(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.SchemeNotPermitted, verdict.Rejection);
    }
}
