using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AntiforgeryEndpoints
{
    public static IEndpointRouteBuilder MapAntiforgeryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/antiforgery", (IAntiforgery antiforgery, HttpContext context) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new AntiforgeryTokenDto(tokens.RequestToken, "X-XSRF-TOKEN"));
        })
        .WithName("Authentication_Antiforgery_Get")
        .Produces<AntiforgeryTokenDto>(StatusCodes.Status200OK)
        .WithSummary("Gets an antiforgery request token.");

        return endpoints;
    }
}
