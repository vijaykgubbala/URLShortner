using System.Buffers.Text;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Domain.Tests.ShortLinks;

/// <summary>
/// T-03, T-04, T-05 — hashing and verification of a management token.
///
/// The rule lives in Domain because deciding whether a presented credential matches a
/// stored one constrains what is valid about domain state — <c>layers.md</c> §3.2. The
/// endpoint extracts a header; it does not decide.
/// </summary>
public class LinkTokenTests
{
    // Built from fixed bytes rather than written as literals, so the fixtures are provably
    // the 32 bytes a real token carries. A hand-typed base64url string is easy to get one
    // character wrong, and a wrong-length token is indistinguishable from a rejected one.
    private static string TokenOf(byte fill) =>
        Base64Url.EncodeToString(Enumerable.Repeat(fill, 32).ToArray());

    private static readonly string Sample = TokenOf(0xA7);
    private static readonly string Other = TokenOf(0x5C);

    // ---- T-03 ----

    [Fact]
    public void Hashing_the_same_token_twice_yields_the_same_bytes()
    {
        Assert.Equal(LinkToken.Hash(Sample), LinkToken.Hash(Sample));
    }

    [Fact]
    public void Two_different_tokens_hash_differently()
    {
        Assert.NotEqual(LinkToken.Hash(Sample), LinkToken.Hash(Other));
    }

    [Fact]
    public void A_hash_is_32_bytes()
    {
        Assert.Equal(32, LinkToken.Hash(Sample).Length);
    }

    /// <summary>
    /// The hash is taken over the decoded bytes, not the encoded text. Hashing the string
    /// makes the stored value depend on the encoding, so a client that pads and a client
    /// that does not produce different hashes for the same secret — the padding-mismatch
    /// bug this pattern is known for.
    /// </summary>
    [Fact]
    public void Padding_does_not_change_the_hash()
    {
        Assert.Equal(LinkToken.Hash(Sample), LinkToken.Hash(Sample + "="));
    }

    // ---- T-04 ----

    [Fact]
    public void Verification_succeeds_for_the_token_that_produced_the_hash()
    {
        Assert.True(LinkToken.Verify(Sample, LinkToken.Hash(Sample)));
    }

    // ---- T-05 ----

    [Fact]
    public void Verification_fails_for_a_different_token()
    {
        Assert.False(LinkToken.Verify(Other, LinkToken.Hash(Sample)));
    }

    /// <summary>
    /// Every malformed shape returns false rather than throwing. An exception here would
    /// escape the handler as a 500, which both leaks that the code exists and breaks the
    /// uniform 404 — <c>layers.md</c> §6.3.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("short")]
    [InlineData("not valid base64url!!!")]
    [InlineData("////++++")]
    public void Verification_fails_without_throwing_for_a_malformed_token(string? presented)
    {
        Assert.False(LinkToken.Verify(presented, LinkToken.Hash(Sample)));
    }

    /// <summary>
    /// A link with no stored hash cannot be mutated by anyone. Rows created before this
    /// feature have a null hash, and the column is nullable per <c>data.md</c> §4.2 — so
    /// "no credential set" must fail closed rather than admit everyone.
    /// </summary>
    [Fact]
    public void Verification_fails_when_no_hash_is_stored()
    {
        Assert.False(LinkToken.Verify(Sample, null));
        Assert.False(LinkToken.Verify(null, null));
    }

    [Fact]
    public void A_token_built_from_32_bytes_is_43_characters()
    {
        Assert.Equal(43, Sample.Length);
    }

    /// <summary>
    /// Hash throws on a malformed token rather than hashing a fallback. It is only ever
    /// called with generator output, so a malformed input is a programming error — and
    /// hashing empty instead would give every malformed token one shared stored hash.
    /// </summary>
    [Fact]
    public void Hashing_a_malformed_token_throws()
    {
        Assert.Throws<ArgumentException>(() => LinkToken.Hash("not-a-token"));
    }

    [Fact]
    public void Verification_fails_for_a_stored_hash_of_the_wrong_length()
    {
        Assert.False(LinkToken.Verify(Sample, [1, 2, 3]));
    }

    /// <summary>
    /// TST-003 — the test the seam could not be. ILinkTokenVerifier proves verification was
    /// *reached*; it cannot see the work inside. An early return here left the seam's count
    /// at 1 and all 217 tests green while reintroducing the timing oracle ADR-002 rests on
    /// closing. This asserts equal work across every input shape, which kills that mutation
    /// without putting a clock in CI.
    /// </summary>
    [Fact]
    public void Every_verification_path_does_the_same_work()
    {
        long Work(Action act)
        {
            var before = System.Threading.Interlocked.Read(ref LinkToken.WorkUnits);
            act();
            return System.Threading.Interlocked.Read(ref LinkToken.WorkUnits) - before;
        }

        var stored = LinkToken.Hash(Sample);

        var authorized = Work(() => LinkToken.Verify(Sample, stored));
        var wrongToken = Work(() => LinkToken.Verify(Other, stored));
        var noStoredHash = Work(() => LinkToken.Verify(Sample, null));
        var malformed = Work(() => LinkToken.Verify("not-a-token", stored));
        var nothing = Work(() => LinkToken.Verify(null, null));

        Assert.Equal(authorized, wrongToken);
        Assert.Equal(authorized, noStoredHash);
        Assert.Equal(authorized, malformed);
        Assert.Equal(authorized, nothing);
        Assert.True(authorized > 0, "the counter never moved -- this test would pass vacuously");
    }
}
