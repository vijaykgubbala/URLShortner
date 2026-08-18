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
