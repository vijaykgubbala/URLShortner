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
    public void Https_is_permitted(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.True(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.None, verdict.Rejection);
    }

    /// <summary>
    /// Review finding SEC-002. "https://user:pw@example.com/path" was an InlineData case on
    /// <see cref="Https_is_permitted"/> above — added to prove credentials do not change the
    /// scheme, which is what T-02 is about, and which is still true. That assertion was
    /// moved here rather than deleted: the URL is now refused, but for a reason that has
    /// nothing to do with its scheme.
    ///
    /// The case that matters is the third one. Uri.Host is "evil.example", so every host
    /// and address rule judges the right target while the Location header a person reads
    /// says paypal.com — the phishing hop STD-SEC-05 exists to prevent.
    /// </summary>
    [Theory]
    [InlineData("https://user:pw@example.com/path")]
    [InlineData("https://user@example.com/path")]
    [InlineData("https://www.paypal.com@evil.example/login")]
    [InlineData("http://accounts.google.com@evil.example/")]
    public void A_userinfo_component_is_refused(string raw)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.False(verdict.IsPermitted);
        Assert.Equal(DestinationRejection.UserInfoNotPermitted, verdict.Rejection);
        Assert.Null(verdict.NormalisedUrl);
    }

    /// <summary>
    /// Review finding COR-001/SEC-003 — the value judged must be the value stored and
    /// emitted. AbsoluteUri is percent-encoded and ASCII, so a destination carrying an
    /// accented path or a CR/LF sequence becomes safe to place in a Location header.
    /// </summary>
    [Theory]
    [InlineData("https://example.com/café", "https://example.com/caf%C3%A9")]
    [InlineData("https://example.com/a\r\nSet-Cookie: x=y", "https://example.com/a%0D%0ASet-Cookie:%20x=y")]
    [InlineData("https://example.com/plain", "https://example.com/plain")]
    public void A_permitted_destination_carries_its_normalised_form(string raw, string expected)
    {
        var verdict = DestinationPolicy.CheckScheme(raw);

        Assert.True(verdict.IsPermitted);
        Assert.Equal(expected, verdict.NormalisedUrl);
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
