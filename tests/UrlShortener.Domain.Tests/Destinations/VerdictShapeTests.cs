using System.Reflection;
using UrlShortener.Domain.Destinations;

namespace UrlShortener.Domain.Tests.Destinations;

/// <summary>
/// T-12 — a scheme-only verdict is a distinct type from a fully-checked one.
///
/// From the brainstorm stress test: the way this design fails is a caller who skips
/// resolution, gets a scheme-only answer, and treats it as a full one. That failure fails
/// OPEN, so the distinction is enforced by the type system rather than by a flag someone
/// has to remember to read.
/// </summary>
public class VerdictShapeTests
{
    [Fact]
    public void Scheme_and_full_verdicts_are_unrelated_types()
    {
        Assert.False(typeof(SchemeVerdict).IsAssignableFrom(typeof(FullVerdict)));
        Assert.False(typeof(FullVerdict).IsAssignableFrom(typeof(SchemeVerdict)));
    }

    [Fact]
    public void A_full_check_cannot_be_satisfied_by_the_scheme_check_alone()
    {
        var full = typeof(DestinationPolicy).GetMethod(nameof(DestinationPolicy.CheckFully));
        var scheme = typeof(DestinationPolicy).GetMethod(nameof(DestinationPolicy.CheckScheme));

        Assert.NotNull(full);
        Assert.NotNull(scheme);
        Assert.Equal(typeof(FullVerdict), full!.ReturnType);
        Assert.Equal(typeof(SchemeVerdict), scheme!.ReturnType);
    }

    [Fact]
    public void A_full_check_requires_a_host_resolution_argument()
    {
        var full = typeof(DestinationPolicy).GetMethod(nameof(DestinationPolicy.CheckFully))!;

        Assert.Contains(full.GetParameters(), p => p.ParameterType == typeof(HostResolution));
    }

    [Fact]
    public void The_domain_predicate_performs_no_io()
    {
        // Pure and synchronous: no method on the policy returns a Task. An async predicate
        // would mean the domain reaching outside the process, which layers.md forbids.
        var asyncMethods = typeof(DestinationPolicy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => typeof(Task).IsAssignableFrom(m.ReturnType))
            .ToList();

        Assert.Empty(asyncMethods);
    }
}
