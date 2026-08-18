using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Destinations;
using UrlShortener.Application.ShortLinks;
using UrlShortener.Domain.ShortLinks;

namespace UrlShortener.Api.ShortLinks;

/// <summary>Transport types live in Entrypoints — <c>layers.md</c> §3.5.</summary>
public sealed record CreateShortLinkRequest(string? Destination);

public sealed record CreateShortLinkResponse(string Code, string Destination, string ShortUrl);

public static class ShortLinkEndpoints
{
    public static void MapShortLinks(this WebApplication app)
    {
        // #18 — api.md §2.1/§2.2: version prefix, kebab-case plural noun, no verb.
        app.MapPost("/v1/short-links", async (
            CreateShortLinkRequest request,
            CreateShortLink useCase,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(request.Destination, ct);
            var traceId = http.TraceIdentifier;

            switch (result.Outcome)
            {
                case CreateOutcome.Created:
                    var url = $"{http.Request.Scheme}://{http.Request.Host}/{result.Code}";
                    return Results.Created(
                        url,
                        new CreateShortLinkResponse(result.Code!, request.Destination!, url));

                case CreateOutcome.DestinationRefused:
                    var problem = DestinationProblem.From(result.Refusal, traceId);
                    return Results.Json(problem, statusCode: problem.Status);

                default:
                    // Not DestinationProblem.Invalid: that factory hard-codes status 400,
                    // so returning it under a 503 put "status": 400 in the body of a 503
                    // response. api.md §4.2 — "status matches the HTTP status code" — and
                    // two conforming clients derived opposite retry behaviour from one
                    // response. The status is now taken from the problem itself.
                    var unavailable = DestinationProblem.Unavailable(traceId);
                    return Results.Json(unavailable, statusCode: unavailable.Status);
            }
        });

        // #19 — the public redirect endpoint. api.md §1.4: this is deliberately not the
        // domain API and carries no version prefix; it performs resolution and nothing else.
        //
        // The length constraint is derived from the domain constant rather than written as
        // a literal, so the route cannot drift from the generator. It also stops the
        // template swallowing every root-level path: /favicon.ico and /robots.txt, which
        // browsers request unprompted, now miss in routing instead of costing a query
        // against the 50 ms p99 budget.
        app.MapGet($"/{{code:length({ShortLink.CodeLength})}}", async (
            [FromRoute] string code,
            ResolveShortLink useCase,
            CancellationToken ct) =>
        {
            // STD-SEC-02 asks for format at the boundary, not only length. A route value
            // outside the code alphabet is refused before it reaches persistence.
            if (!ShortLink.IsWellFormedCode(code))
            {
                return Results.NotFound();
            }

            var result = await useCase.ExecuteAsync(code, ct);

            return result.Outcome switch
            {
                ResolveOutcome.Found => Results.Redirect(result.Destination!, permanent: false),
                ResolveOutcome.NoLongerPermitted => Results.StatusCode(410),
                _ => Results.NotFound()
            };
        });
    }
}
