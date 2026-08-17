using System.Security.Cryptography;
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
        catch (DbUpdateException)
        {
            // The database rejected the code as a duplicate. Two concurrent creates race
            // here rather than at a check-then-insert, which is the point: only one wins,
            // and the loser retries with a new code instead of failing the request.
            db.Entry(link).State = EntityState.Detached;
            return false;
        }
    }

    public Task<ShortLink?> FindAsync(string code, CancellationToken cancellationToken) =>
        db.ShortLinks.AsNoTracking().FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
}

/// <summary>
/// architecture/data.md §1.4 — codes come from a cryptographically secure source, never a
/// sequence. Sequential codes make the whole link set enumerable.
/// </summary>
public sealed class CryptoShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public string Next()
    {
        var chars = new char[ShortLink.CodeLength];

        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }
}
