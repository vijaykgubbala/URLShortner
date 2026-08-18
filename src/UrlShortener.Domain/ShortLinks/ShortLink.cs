namespace UrlShortener.Domain.ShortLinks;

/// <summary>
/// Generates short codes. Declared in Domain because the code format is a domain rule
/// (<c>layers.md</c> §3.2) and randomness must come through an interface (§3.6) so the
/// domain stays testable without a random source.
/// </summary>
public interface IShortCodeGenerator
{
    string Next();
}

/// <summary>
/// Generates management tokens. Declared here for the same reason as
/// <see cref="IShortCodeGenerator"/> — <c>layers.md</c> §3.6 requires a random source to be
/// reached through an interface so the domain stays testable without one.
///
/// Separate from <see cref="IShortCodeGenerator"/> rather than a second method on it: a
/// short code is a public identifier and a token is a credential, and a single interface
/// producing both invites a caller to reach for the wrong one.
/// </summary>
public interface ILinkTokenGenerator
{
    /// <summary>A new token. Returned to its creator once and never recoverable after.</summary>
    string Next();
}

/// <summary>A stored short link. The code is its public identity — <c>architecture/api.md</c> §2.4.</summary>
public sealed class ShortLink
{
    public const int CodeLength = 7;

    /// <summary>
    /// The characters a code is drawn from. Lives here rather than beside the generator
    /// because the code format is a domain rule (<c>layers.md</c> §3.2) — Infrastructure
    /// supplies the randomness, Domain owns the shape. Keeping both halves here is what
    /// lets the trust boundary reject a malformed code without restating the format.
    /// </summary>
    public const string CodeAlphabet =
        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Whether a caller-supplied string could be a code this system minted. Used at the
    /// trust boundary, so an arbitrary path segment never reaches persistence.
    /// </summary>
    public static bool IsWellFormedCode(string? code) =>
        code is { Length: CodeLength } && code.All(CodeAlphabet.Contains);

    private ShortLink() { }   // EF Core

    public ShortLink(string code, string destination, DateTimeOffset createdAt)
    {
        Code = code;
        Destination = destination;
        CreatedAt = createdAt;
    }

    public string Code { get; private set; } = string.Empty;

    public string Destination { get; private set; } = string.Empty;

    /// <summary>UTC, per <c>architecture/data.md</c> §1.6.</summary>
    public DateTimeOffset CreatedAt { get; private set; }
}
