using Microsoft.Extensions.Logging;
using UrlShortener.Application.Destinations;
using UrlShortener.Domain.Destinations;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Application.ShortLinks;

/// <summary>Persistence for short links. Declared in Application, which consumes it — <c>layers.md</c> §4.1.</summary>
public interface IShortLinkRepository
{
    /// <summary>Returns false if the code is already taken — the database decides, not a prior read.</summary>
    Task<bool> TryAddAsync(ShortLink link, CancellationToken cancellationToken);

    Task<ShortLink?> FindAsync(string code, CancellationToken cancellationToken);
}

public enum CreateOutcome { Created, DestinationRefused, CodeExhausted }

public sealed record CreateResult(CreateOutcome Outcome, string? Code, DestinationRefusal Refusal);

public enum ResolveOutcome { Found, NotFound, NoLongerPermitted }

public sealed record ResolveResult(ResolveOutcome Outcome, string? Destination);

/// <summary>#18 — create a short link.</summary>
public sealed class CreateShortLink(
    IShortLinkRepository repository,
    IShortCodeGenerator codes,
    ValidateDestination validator,
    TimeProvider clock,
    ILogger<CreateShortLink>? logger = null,
    CreateFailureCounter? counter = null)
{
    /// <summary>
    /// Attempts on collision. Uniqueness is enforced by the database constraint rather than
    /// a check-then-insert, per <c>architecture/data.md</c> §1.5 — a prior read does not
    /// survive two concurrent creates.
    /// </summary>
    private const int MaxAttempts = 5;

    public async Task<CreateResult> ExecuteAsync(string? destination, CancellationToken cancellationToken)
    {
        var validation = await validator.ExecuteAsync(destination, cancellationToken).ConfigureAwait(false);

        if (!validation.IsPermitted)
        {
            return new CreateResult(CreateOutcome.DestinationRefused, null, validation.Refusal);
        }

        // The normalised form the Domain judged, never the caller's raw string: Uri
        // .AbsoluteUri is percent-encoded and ASCII, so what is stored is emittable in a
        // Location header by construction. Storing the raw text means the value checked
        // and the value later redirected to are different strings.
        var destinationToStore = validation.NormalisedUrl!;

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var link = new ShortLink(codes.Next(), destinationToStore, clock.GetUtcNow());

            if (await repository.TryAddAsync(link, cancellationToken).ConfigureAwait(false))
            {
                return new CreateResult(CreateOutcome.Created, link.Code, DestinationRefusal.None);
            }
        }

        counter?.Increment();
        logger?.LogError(
            new EventId(1100, "link.create.failed"),
            "Could not allocate a unique short code in {Attempts} attempts.",
            MaxAttempts);

        return new CreateResult(CreateOutcome.CodeExhausted, null, DestinationRefusal.None);
    }
}

/// <summary>#19 — resolve a short code.</summary>
public sealed class ResolveShortLink(
    IShortLinkRepository repository,
    ILogger<ResolveShortLink>? logger = null,
    ResolveFailureCounter? counter = null)
{
    public async Task<ResolveResult> ExecuteAsync(string code, CancellationToken cancellationToken)
    {
        var link = await repository.FindAsync(code, cancellationToken).ConfigureAwait(false);

        if (link is null)
        {
            return new ResolveResult(ResolveOutcome.NotFound, null);
        }

        // Re-check the scheme before emitting a Location header. STD-SEC-05 wants the full
        // check here; WVR-001 waives the address half for the p99 budget, so this is the
        // scheme half — a destination stored before a policy change is still refused.
        if (!DestinationPolicy.CheckScheme(link.Destination).IsPermitted)
        {
            counter?.Increment();
            logger?.LogWarning(
                new EventId(1101, "redirect.resolve.failed"),
                "Stored destination for {Code} is no longer permitted.",
                code);

            return new ResolveResult(ResolveOutcome.NoLongerPermitted, null);
        }

        return new ResolveResult(ResolveOutcome.Found, link.Destination);
    }
}

/// <summary>AC: `link_create_failures` — <c>STD-OPS-02</c>.</summary>
public sealed class CreateFailureCounter
{
    private int _total;
    public int Total => Volatile.Read(ref _total);
    public void Increment() => Interlocked.Increment(ref _total);
}

/// <summary>AC: `redirect_failures` — <c>STD-OPS-02</c>.</summary>
public sealed class ResolveFailureCounter
{
    private int _total;
    public int Total => Volatile.Read(ref _total);
    public void Increment() => Interlocked.Increment(ref _total);
}
