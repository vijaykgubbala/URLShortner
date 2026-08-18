using System.Reflection;
using UrlShortener.Api.ShortLinks;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Api.Tests.ShortLinks;

/// <summary>
/// T-14 — AC-4: "Given any response other than the original create, when it is returned,
/// then it does not contain the token."
///
/// **"Any response" is a category, not a list.** Asserting it endpoint by endpoint tests
/// the three responses that exist today and agrees with the implementation forever — which
/// is exactly how #17 shipped an address check that enumerated four examples of "reserved"
/// and left multicast permitted. The lesson from that
/// (`docs/solutions/validation/enumerate-the-category-not-the-examples.md`) prescribes
/// expressing a range as a range, and this is that prescription applied to a response
/// surface: enumerate the assembly, not the endpoints.
///
/// This is load-bearing rather than theoretical. #22 "Query the link collection" is already
/// on the board, and the obvious implementation — returning records shaped from the entity —
/// would put the token hash on the wire past any per-endpoint test.
/// </summary>
public class ResponseSurfaceTests
{
    /// <summary>Every public type the Api assembly could serialise.</summary>
    private static IEnumerable<Type> ApiTypes() =>
        typeof(CreateShortLinkResponse).Assembly
            .GetTypes()
            .Where(t => t.IsPublic
                        && t.Namespace?.StartsWith("UrlShortener.Api", StringComparison.Ordinal) == true);

    private static IEnumerable<PropertyInfo> PropertiesOf(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

    /// <summary>
    /// Walks property types transitively. A one-level scan would miss a hash reached through
    /// a nested type — `DestinationProblem.Errors` is an `IReadOnlyList&lt;FieldError&gt;`,
    /// so nesting is already how this surface is shaped.
    /// </summary>
    private static IEnumerable<(Type Owner, PropertyInfo Property)> ReachableProperties()
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>(ApiTypes());

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();

            if (!seen.Add(type))
            {
                continue;
            }

            foreach (var property in PropertiesOf(type))
            {
                yield return (type, property);

                foreach (var candidate in Unwrap(property.PropertyType))
                {
                    if (candidate.Namespace?.StartsWith("UrlShortener", StringComparison.Ordinal) == true)
                    {
                        queue.Enqueue(candidate);
                    }
                }
            }
        }
    }

    /// <summary>The type itself plus any generic arguments, so collections are followed.</summary>
    private static IEnumerable<Type> Unwrap(Type type) =>
        new[] { type }.Concat(type.IsGenericType ? type.GetGenericArguments() : []);

    /// <summary>
    /// No serialisable API type may expose the stored hash. The hash is not the credential,
    /// so leaking it is not immediately fatal — but it hands an attacker a free offline
    /// oracle to verify guesses against, and there is no reason for it to leave the process.
    /// </summary>
    [Fact]
    public void No_api_response_type_exposes_a_token_hash()
    {
        var offenders = ReachableProperties()
            .Where(p => p.Property.Name.Contains("TokenHash", StringComparison.OrdinalIgnoreCase)
                        || p.Property.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase))
            .Select(p => $"{p.Owner.Name}.{p.Property.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"hash reachable from the API surface: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// The realistic leak is not a DTO growing a hash property — it is an endpoint returning
    /// the entity. `layers.md` §5.2/§5.3 already forbid mapping a Domain type onto the
    /// published contract; this asserts it, so the rule fails a build rather than a review.
    /// </summary>
    [Fact]
    public void No_api_type_exposes_the_domain_entity()
    {
        var offenders = ReachableProperties()
            .Where(p => Unwrap(p.Property.PropertyType).Contains(typeof(ShortLink)))
            .Select(p => $"{p.Owner.Name}.{p.Property.Name}")
            .ToList();

        Assert.True(offenders.Count == 0, $"ShortLink reachable from the API surface: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// The create response is the one permitted exception, and it is asserted rather than
    /// assumed — so deleting the token from it fails here rather than silently voiding AC-1.
    /// </summary>
    [Fact]
    public void Exactly_one_api_type_carries_a_token_and_it_is_the_create_response()
    {
        var carriers = ReachableProperties()
            .Where(p => p.Property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase))
            .Select(p => $"{p.Owner.Name}.{p.Property.Name}")
            .ToList();

        Assert.Equal([$"{nameof(CreateShortLinkResponse)}.ManagementToken"], carriers);
    }

    /// <summary>
    /// Proves the walker actually reaches things. Without this, a bug that made
    /// ReachableProperties return nothing would make all three assertions above pass
    /// vacuously — a green suite proving the absence of a surface it never looked at.
    /// </summary>
    [Fact]
    public void The_walker_reaches_the_known_response_surface()
    {
        var reached = ReachableProperties().Select(p => $"{p.Owner.Name}.{p.Property.Name}").ToList();

        Assert.Contains($"{nameof(CreateShortLinkResponse)}.Code", reached);
        Assert.Contains("DestinationProblem.TraceId", reached);
        Assert.Contains("FieldError.Field", reached);
    }
}
