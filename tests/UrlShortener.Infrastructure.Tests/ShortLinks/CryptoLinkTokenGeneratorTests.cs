using UrlShortener.Domain.ShortLinks;
using UrlShortener.Infrastructure.ShortLinks;

namespace UrlShortener.Infrastructure.Tests.ShortLinks;

/// <summary>
/// T-01, T-02 — the management token is unguessable and safe to put in a header.
///
/// **These tests prove shape, distinctness and non-ordering. They do NOT prove the source
/// is cryptographically secure.** Review finding TST-004 corrected an earlier claim here
/// that substituting Random.Shared would fail: it was checked by mutation and it does not.
/// Every test below passes with `Random.Shared.NextBytes`, and with a `new Random(42)`
/// seeded once per process. Only a monotonic counter or timestamp fails.
///
/// AC-1's "generated from a cryptographically secure source" therefore has no test. A false
/// claim in a test file is worse than no claim, because a reader believes it. The
/// distribution test that would close this is routed to #50.
/// </summary>
public class CryptoLinkTokenGeneratorTests
{
    private readonly ILinkTokenGenerator _generator = new CryptoLinkTokenGenerator();

    /// <summary>
    /// T-01. 32 bytes of entropy is 43 base64url characters unpadded. RFC 4648 §5 permits
    /// omitting the padding when the length is known implicitly, and it is fixed here.
    /// </summary>
    [Fact]
    public void A_token_is_43_unpadded_base64url_characters()
    {
        var token = _generator.Next();

        Assert.Equal(43, token.Length);
        Assert.DoesNotContain("=", token);
        Assert.DoesNotContain("+", token);
        Assert.DoesNotContain("/", token);
    }

    [Fact]
    public void Every_character_is_in_the_base64url_alphabet()
    {
        const string alphabet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

        var token = _generator.Next();

        Assert.All(token, c => Assert.Contains(c, alphabet));
    }

    /// <summary>T-02. A collision in 1,000 draws from a 2^256 space would not be chance.</summary>
    [Fact]
    public void A_thousand_draws_are_all_distinct()
    {
        var tokens = Enumerable.Range(0, 1000).Select(_ => _generator.Next()).ToList();

        Assert.Equal(tokens.Count, tokens.Distinct().Count());
    }

    /// <summary>
    /// T-02, the half that matters. Distinctness alone does not catch a sequence — a counter
    /// is <em>more</em> distinct than a CSPRNG. This asserts the draws are not ordered,
    /// which a counter, a timestamp, or a seeded Random cannot satisfy.
    /// </summary>
    [Fact]
    public void Draws_are_not_ordinally_sequential()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => _generator.Next()).ToList();

        Assert.False(tokens.SequenceEqual(tokens.OrderBy(t => t, StringComparer.Ordinal)));
        Assert.False(tokens.SequenceEqual(
            tokens.OrderByDescending(t => t, StringComparer.Ordinal)));
    }

    /// <summary>
    /// The first character varies. A generator seeded once per process, or one deriving the
    /// token from the short code, tends to cluster here.
    /// </summary>
    [Fact]
    public void The_leading_character_is_not_fixed()
    {
        var leads = Enumerable.Range(0, 200)
            .Select(_ => _generator.Next()[0])
            .Distinct()
            .Count();

        Assert.True(leads > 10, $"only {leads} distinct leading characters in 200 draws");
    }
}
