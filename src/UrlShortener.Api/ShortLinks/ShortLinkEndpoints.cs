using Microsoft.AspNetCore.Mvc;
using UrlShortener.Api.Destinations;
using UrlShortener.Application.ShortLinks;

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
                    return Results.Json(
                        DestinationProblem.Invalid(
                            [("destination", "a unique short code could not be allocated")], traceId),
                        statusCode: 503);
            }
        });

        // #19 — the public redirect endpoint. api.md §1.4: this is deliberately not the
        // domain API and carries no version prefix; it performs resolution and nothing else.
        app.MapGet("/{code}", async (
            [FromRoute] string code,
            ResolveShortLink useCase,
            CancellationToken ct) =>
        {
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
