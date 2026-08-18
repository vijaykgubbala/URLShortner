using UrlShortener.Api.Destinations;
using UrlShortener.Application.Destinations;

namespace UrlShortener.Api.Tests.Destinations;

/// <summary>
/// T-19 … T-23 — the published error contract.
///
/// api.md §4.2 requires a stable kebab-case type; §4.3 forbids internal detail in the
/// body; §4.4 requires 400 field failures reported together; §4.5 puts a domain-rule
/// violation at 422; §4.6 requires a traceId on every error.
/// </summary>
public class ErrorContractTests
{
    private const string Trace = "0af7651916cd43dd8448eb211c80319c";

    // T-19 — policy refusals are 422 with a stable kebab-case type
    [Theory]
    [InlineData(DestinationRefusal.SchemeNotPermitted, "destination-scheme-not-permitted")]
    [InlineData(DestinationRefusal.AddressNotPermitted, "destination-address-not-permitted")]
    [InlineData(DestinationRefusal.HostNotResolved, "destination-host-not-resolved")]
    [InlineData(DestinationRefusal.ResolutionFailed, "destination-resolution-failed")]
    public void A_policy_refusal_is_422_with_a_kebab_case_type(
        DestinationRefusal refusal, string expectedType)
    {
        var problem = DestinationProblem.From(refusal, Trace);

        Assert.Equal(422, problem.Status);
        Assert.Equal(expectedType, problem.Type);
    }

    // T-20 — an unparseable value is 400 naming the offending field
    [Fact]
    public void An_unparseable_value_is_400_naming_the_field()
    {
        var problem = DestinationProblem.From(DestinationRefusal.NotAbsoluteUrl, Trace);

        Assert.Equal(400, problem.Status);
        Assert.Equal("invalid-request", problem.Type);
        Assert.Contains(problem.Errors, e => e.Field == "destination");
    }

    // T-23 — 400 field failures are reported together, not one at a time
    [Fact]
    public void Field_failures_are_reported_together()
    {
        var problem = DestinationProblem.Invalid(
            [("destination", "must be an absolute http or https URL"),
             ("expiresAt", "must be in the future")],
            Trace);

        Assert.Equal(400, problem.Status);
        Assert.Equal(2, problem.Errors.Count);
        Assert.Contains(problem.Errors, e => e.Field == "destination");
        Assert.Contains(problem.Errors, e => e.Field == "expiresAt");
    }

    // T-22 — every error carries a traceId
    [Theory]
    [InlineData(DestinationRefusal.NotAbsoluteUrl)]
    [InlineData(DestinationRefusal.SchemeNotPermitted)]
    [InlineData(DestinationRefusal.AddressNotPermitted)]
    public void Every_error_carries_the_trace_identifier(DestinationRefusal refusal)
    {
        Assert.Equal(Trace, DestinationProblem.From(refusal, Trace).TraceId);
    }

    /// <summary>
    /// T-21 — no error body carries a resolved address or any internal detail.
    /// api.md §4.3. The address the host resolved to belongs in the log, correlated by
    /// traceId, never in a response that tells a stranger about our network.
    /// </summary>
    [Theory]
    [InlineData(DestinationRefusal.AddressNotPermitted)]
    [InlineData(DestinationRefusal.HostNotResolved)]
    [InlineData(DestinationRefusal.ResolutionFailed)]
    public void No_error_body_reveals_an_address_or_internal_detail(DestinationRefusal refusal)
    {
        var problem = DestinationProblem.From(refusal, Trace);
        var body = $"{problem.Type} {problem.Title} {problem.Detail}";

        Assert.DoesNotContain("10.", body);
        Assert.DoesNotContain("127.0", body);
        Assert.DoesNotContain("192.168", body);
        Assert.DoesNotContain("Exception", body);
        Assert.DoesNotContain("::", body);
        Assert.DoesNotContain("\\", body);
    }

    /// <summary>
    /// The published type strings are chosen deliberately, not projected from an enum.
    /// api.md §4.2 — "must not change once published". If these were ToString() of the
    /// refusal, renaming a member would silently break every consumer.
    /// </summary>
    [Fact]
    public void Published_types_are_not_the_enum_names()
    {
        foreach (var refusal in Enum.GetValues<DestinationRefusal>())
        {
            if (refusal == DestinationRefusal.None) continue;

            var type = DestinationProblem.From(refusal, Trace).Type;

            Assert.NotEqual(refusal.ToString(), type);
            Assert.Equal(type.ToLowerInvariant(), type);
            Assert.DoesNotContain(" ", type);
            Assert.DoesNotContain("_", type);
        }
    }

    [Fact]
    public void A_permitted_destination_produces_no_problem()
    {
        Assert.Null(DestinationProblem.FromOrNull(DestinationRefusal.None, Trace));
    }

    /// <summary>
    /// Review finding H1 — raised independently by the testing, correctness and
    /// architecture lenses. Code exhaustion was reported through DestinationProblem
    /// .Invalid, which hard-codes 400, under an HTTP 503. api.md §4.2: "status matches the
    /// HTTP status code." A client reading the body stopped retrying; a client reading the
    /// status line retried. Both were conforming.
    /// </summary>
    [Fact]
    public void An_allocation_failure_carries_a_server_status_that_matches_its_body()
    {
        var problem = DestinationProblem.Unavailable(Trace);

        Assert.Equal(503, problem.Status);
        Assert.NotEqual("invalid-request", problem.Type);
        Assert.Empty(problem.Errors);
        Assert.Equal(Trace, problem.TraceId);
    }
}
