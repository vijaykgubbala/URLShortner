using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Application.ShortLinks;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Infrastructure.ShortLinks;

public sealed class ShortLinkDbContext(DbContextOptions<ShortLinkDbContext> options) : DbContext(options)
{
    public DbSet<ShortLink> ShortLinks => Set<ShortLink>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var link = b.Entity<ShortLink>();
        link.HasKey(l => l.Code);
        link.Property(l => l.Code).HasMaxLength(ShortLink.CodeLength).IsRequired();
        link.Property(l => l.Destination).HasMaxLength(2048).IsRequired();
        link.Property(l => l.CreatedAt).IsRequired();

        // architecture/data.md §1.5 — uniqueness is enforced here, not by a prior read.
        link.HasIndex(l => l.Code).IsUnique();
    }
}

public sealed class EfShortLinkRepository(ShortLinkDbContext db) : IShortLinkRepository
{
    public async Task<bool> TryAddAsync(ShortLink link, CancellationToken cancellationToken)
    {
        db.ShortLinks.Add(link);

        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniquenessViolation(ex))
        {
            // The database rejected the code as a duplicate. Two concurrent creates race
            // here rather than at a check-then-insert, which is the point: only one wins,
            // and the loser retries with a new code instead of failing the request.
            //
            // The filter matters as much as the catch. DbUpdateException wraps every
            // SaveChanges failure — a dropped connection, a full disk, SQLITE_BUSY, a
            // concurrency conflict — and returning false for those reports "this code is
            // taken", so the caller retries a failing write five times and then tells the
            // operator the 62^7 code space is exhausted. Every other fault propagates.
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    /// <summary>
    /// A genuine PRIMARY KEY or UNIQUE violation, and nothing else. SQLITE_CONSTRAINT is
    /// 19; the extended codes distinguish which constraint failed
    /// (1555 = PRIMARY KEY, 2067 = UNIQUE).
    /// </summary>
    private static bool IsUniquenessViolation(DbUpdateException ex) =>
        ex.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 1555 or 2067
        };

    public Task<ShortLink?> FindAsync(string code, CancellationToken cancellationToken) =>
        db.ShortLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
}

/// <summary>
/// Management tokens, 32 bytes of CSPRNG output as unpadded base64url — 43 characters.
///
/// 256 bits puts brute force outside the threat model entirely, which is what lets the
/// stored hash be a plain SHA-256 rather than a slow KDF: NIST SP 800-63B and OWASP ASVS
/// §6.5.2 both make the salted-KDF requirement conditional on being *below* 112 bits.
/// A slow KDF here would also hand an unauthenticated caller a per-request CPU amplifier.
/// </summary>
public sealed class CryptoLinkTokenGenerator : ILinkTokenGenerator
{
    private const int TokenBytes = 32;

    public string Next()
    {
        Span<byte> raw = stackalloc byte[TokenBytes];
        RandomNumberGenerator.Fill(raw);

        // Base64Url is built into .NET 9 and omits padding natively — no manual
        // Replace('+','-') surgery, which is where encoding bugs in this pattern live.
        return Base64Url.EncodeToString(raw);
    }
}

/// <summary>
/// architecture/data.md §1.4 — codes come from a cryptographically secure source, never a
/// sequence. Sequential codes make the whole link set enumerable.
/// </summary>
public sealed class CryptoShortCodeGenerator : IShortCodeGenerator
{
    public string Next()
    {
        var chars = new char[ShortLink.CodeLength];

        for (var i = 0; i < chars.Length; i++)
        {
            // The alphabet is the Domain's, not this class's — the boundary guard in
            // Entrypoints and this generator must agree by construction, not by copy.
            chars[i] = ShortLink.CodeAlphabet[
                RandomNumberGenerator.GetInt32(ShortLink.CodeAlphabet.Length)];
        }

        return new string(chars);
    }
}
