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

    /// <summary>Returns false if no row was removed — the database decides, not a prior read.</summary>
    Task<bool> TryDeleteAsync(string code, CancellationToken cancellationToken);
}

public enum CreateOutcome { Created, DestinationRefused, CodeExhausted }

/// <summary>
/// <c>ManagementToken</c> is the plaintext, present only on a Created outcome and only in
/// memory. It is never persisted -- only its hash is -- and never logged.
/// </summary>
public sealed record CreateResult(
    CreateOutcome Outcome, string? Code, DestinationRefusal Refusal, string? ManagementToken)
{
    /// <summary>
    /// Review finding SEC-001. A positional record synthesises ToString over every public
    /// property, so any debug dump or `logger.LogInformation("{Result}", result)` printed
    /// the token in cleartext -- STD-SEC-03's "logged by a default formatter" clause, whose
    /// detection hint names a ToString override exactly. The doc comment claiming the token
    /// is never logged was a promise; this is the control.
    ///
    /// The default on ManagementToken is also gone (COR-003): the plan required the
    /// equivalent parameter on ShortLink be mandatory because "an optional default silently
    /// creates links with no credential", and the same hazard applied here.
    /// </summary>
    private bool PrintMembers(System.Text.StringBuilder builder)
    {
        builder.Append($"Outcome = {Outcome}, Code = {Code}, Refusal = {Refusal}");
        return true;
    }
}

public enum ResolveOutcome { Found, NotFound, NoLongerPermitted }

/// <summary>
/// Two outcomes, not three. An unknown code and a wrong token both report
/// <c>Refused</c> — the caller must not be able to tell them apart, and the surest way to
/// keep that true is to give the endpoint nothing to tell them apart with. ADR-002.
/// </summary>
public enum DeleteOutcome { Deleted, Refused }

public sealed record DeleteResult(DeleteOutcome Outcome);

public sealed record ResolveResult(ResolveOutcome Outcome, string? Destination);

/// <summary>#18 — create a short link.</summary>
public sealed class CreateShortLink(
    IShortLinkRepository repository,
    IShortCodeGenerator codes,
    ILinkTokenGenerator tokens,
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
            return new CreateResult(CreateOutcome.DestinationRefused, null, validation.Refusal, null);
        }

        // The normalised form the Domain judged, never the caller's raw string: Uri
        // .AbsoluteUri is percent-encoded and ASCII, so what is stored is emittable in a
        // Location header by construction. Storing the raw text means the value checked
        // and the value later redirected to are different strings.
        var destinationToStore = validation.NormalisedUrl!;

        // One token per link, minted once. It leaves this method in the result and is
        // never written anywhere but the response -- what the row carries is its hash.
        var token = tokens.Next();
        var tokenHash = LinkToken.Hash(token);

        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var link = new ShortLink(
                codes.Next(), destinationToStore, clock.GetUtcNow(), tokenHash);

            if (await repository.TryAddAsync(link, cancellationToken).ConfigureAwait(false))
            {
                return new CreateResult(
                    CreateOutcome.Created, link.Code, DestinationRefusal.None, token);
            }
        }

        counter?.Increment();
        logger?.LogError(
            new EventId(1100, "link.create.failed"),
            "Could not allocate a unique short code in {Attempts} attempts.",
            MaxAttempts);

        return new CreateResult(CreateOutcome.CodeExhausted, null, DestinationRefusal.None, null);
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

/// <summary>#21 — delete a short link, on presentation of its management token.</summary>
public sealed class DeleteShortLink(
    IShortLinkRepository repository,
    ILinkTokenVerifier verifier,
    ILogger<DeleteShortLink>? logger = null,
    DeleteRefusalCounter? counter = null)
{
    public async Task<DeleteResult> ExecuteAsync(
        string code, string? presentedToken, CancellationToken cancellationToken)
    {
        // Review findings COR-002, ARCH-006 and TST-010. This guard used to sit in the
        // endpoint, which short-circuited to Refused without entering this method -- so that
        // refusal emitted no log line and moved no counter, while the endpoint still
        // returned a 404 carrying a traceId that pointed at nothing. The release notes tell
        // support to ask for that identifier, so the guard belongs here, where every refusal
        // takes one path. A malformed code still costs no database round trip.
        var wellFormed = ShortLink.IsWellFormedCode(code);

        var link = wellFormed
            ? await repository.FindAsync(code, cancellationToken).ConfigureAwait(false)
            : null;

        // Verification runs BEFORE any branch on whether the link exists, and runs even when
        // it does not. An early return here would be behaviourally identical -- same
        // outcome, same 404, same body -- and differ only in duration, which is enough to
        // tell a prober which codes are real. That is the enumeration oracle ADR-002 exists
        // to close, and closing it in the status code while leaving it in the timing closes
        // nothing. `link?.TokenHash` is null for a missing link, and Verify fails closed on
        // null without shortcutting.
        var authorized = verifier.Verify(presentedToken, link?.TokenHash);

        if (!wellFormed || link is null || !authorized)
        {
            counter?.Increment();

            // The code, never the token. STD-SEC-04 -- and the code is already in the
            // request path, so it discloses nothing the caller did not send.
            logger?.LogWarning(
                new EventId(1102, "link.delete.refused"),
                "Delete refused for {Code}.",
                code);

            return new DeleteResult(DeleteOutcome.Refused);
        }

        // Review finding COR-001. This result used to be discarded, making Deleted an
        // unverified claim: a second concurrent delete removed nothing and still reported
        // 204. The interface's contract is "returns false if no row was removed -- the
        // database decides, not a prior read", and dropping the value made that contract
        // unobservable at every level, so no test could detect it regressing.
        var removed = await repository
            .TryDeleteAsync(code, cancellationToken).ConfigureAwait(false);

        if (!removed)
        {
            counter?.Increment();
            logger?.LogWarning(
                new EventId(1102, "link.delete.refused"),
                "Delete refused for {Code}.",
                code);
        }

        return new DeleteResult(removed ? DeleteOutcome.Deleted : DeleteOutcome.Refused);
    }
}

/// <summary>AC: `link_delete_refusals` — <c>STD-OPS-02</c>.</summary>
public sealed class DeleteRefusalCounter
{
    private int _total;
    public int Total => Volatile.Read(ref _total);
    public void Increment() => Interlocked.Increment(ref _total);
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
