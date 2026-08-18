using System.Buffers.Text;
using System.Security.Cryptography;

namespace UrlShortener.Domain.ShortLinks;

/// <summary>
/// Verifies a presented token against a stored hash.
///
/// This interface exists for one reason: to make the constant-work property *provable*.
/// <see cref="LinkToken.Verify"/> deliberately hashes and compares on every path, including
/// when no link was found — because an early return there would make an unknown code
/// measurably faster than a wrong token, leaking through timing what the identical 404
/// conceals (<c>ADR-002</c>).
///
/// An early return is *behaviourally identical* — same outcome, same status, same body —
/// so no outcome-based test can catch its absence. A seam can: a counting fake proves the
/// use case reached verification even when the code did not exist. One interface with one
/// implementation is the cost of that proof, and the security argument in ADR-002 rests on
/// the property holding.
/// </summary>
public interface ILinkTokenVerifier
{
    bool Verify(string? presented, byte[]? storedHash);
}

/// <summary>The production verifier. Delegates to <see cref="LinkToken"/>.</summary>
public sealed class LinkTokenVerifier : ILinkTokenVerifier
{
    public bool Verify(string? presented, byte[]? storedHash) =>
        LinkToken.Verify(presented, storedHash);
}

/// <summary>
/// Hashes and verifies a management token.
///
/// Lives in Domain because deciding whether a presented credential matches a stored one
/// constrains what is valid about domain state — <c>layers.md</c> §3.2, which requires such
/// a rule to be "implemented in Domain, on the entity or in a domain service" and "not
/// implemented only in a handler, controller, or view model". The endpoint extracts a
/// header value and maps an outcome; it decides nothing.
/// </summary>
public static class LinkToken
{
    private const int HashBytes = 32;

    /// <summary>
    /// A dummy compared against when there is nothing real to compare against, so the
    /// not-found and wrong-token paths do the same work. See <see cref="Verify"/>.
    /// </summary>
    private static readonly byte[] Absent = new byte[HashBytes];

    /// <summary>
    /// Counts hash-and-compare operations, so a test can assert that every verification path
    /// does the same amount of work without putting a clock in CI.
    ///
    /// Review finding TST-003. The <see cref="ILinkTokenVerifier"/> seam proves verification
    /// was *reached*; it cannot see what happens inside. An early return here -- for example
    /// `if (storedHash is null) return false;` -- left the seam's count at 1 and the whole
    /// suite green while reintroducing exactly the timing oracle ADR-002 rests on closing.
    /// This counter is what kills that mutation.
    /// </summary>
    internal static long WorkUnits;

    /// <summary>
    /// SHA-256 over the token's decoded bytes.
    ///
    /// A plain hash rather than a slow KDF is correct here and is not a shortcut: NIST
    /// SP 800-63B and OWASP ASVS §6.5.2 both make the salted-KDF requirement conditional on
    /// the secret carrying fewer than 112 bits of entropy. A token is 256 bits from a
    /// CSPRNG, so there is no dictionary to attack and no salt to add — two random tokens
    /// colliding is a non-event, and there is no rainbow table for a 2^256 space. A slow KDF
    /// would also let an unauthenticated caller spend server CPU per wrong guess.
    ///
    /// Hashing the <em>decoded bytes</em> rather than the string is what keeps the stored
    /// value independent of the encoding: a padded and an unpadded form of the same secret
    /// must produce the same hash, or half the clients fail to authenticate.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The token is not well formed. This throws rather than hashing a fallback, because
    /// <see cref="Hash"/> is only ever called with freshly generated output — a malformed
    /// input here is a programming error, and hashing empty instead would give every
    /// malformed token the same stored hash. <see cref="Verify"/> is the method that must
    /// tolerate arbitrary input, and it does.
    /// </exception>
    public static byte[] Hash(string token) =>
        SHA256.HashData(
            Decode(token)
            ?? throw new ArgumentException("Not a well-formed token.", nameof(token)));

    /// <summary>
    /// Whether the presented token matches the stored hash.
    ///
    /// **This method deliberately performs the same work on every path.** It hashes and runs
    /// a fixed-time comparison even when the token is malformed or no hash is stored,
    /// comparing against <see cref="Absent"/> instead. Returning early on those paths would
    /// make an unknown code measurably faster than a wrong token — and the two are supposed
    /// to be indistinguishable, so a difference in duration hands back through timing
    /// exactly what the identical 404 conceals. See
    /// <c>ADR-002</c>.
    ///
    /// Fails closed on every uncertain input: a null or malformed token, and a stored hash
    /// that is null or the wrong length, all return false. A row with no credential set
    /// cannot be mutated by anyone rather than by everyone.
    /// </summary>
    public static bool Verify(string? presented, byte[]? storedHash)
    {
        var presentedHash = SHA256.HashData(Decode(presented) ?? []);
        Interlocked.Increment(ref WorkUnits);

        var comparand = storedHash is { Length: HashBytes } ? storedHash : Absent;

        var matches = CryptographicOperations.FixedTimeEquals(presentedHash, comparand);
        Interlocked.Increment(ref WorkUnits);

        // The comparison ran either way; its result only counts when there was something
        // real to compare against and the input was well formed.
        return matches && storedHash is { Length: HashBytes } && Decode(presented) is not null;
    }

    /// <summary>
    /// The token's bytes, or null if it is not a well-formed token. Accepts the padded form
    /// as well as the unpadded one the generator emits, so a client that adds padding is not
    /// silently rejected.
    /// </summary>
    private static byte[]? Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var trimmed = token.TrimEnd('=');

        if (!Base64Url.IsValid(trimmed))
        {
            return null;
        }

        try
        {
            var bytes = Base64Url.DecodeFromChars(trimmed);
            return bytes.Length == HashBytes ? bytes : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
