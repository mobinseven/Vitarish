using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AntiforgeryEndpoints
{
    public static RouteGroupBuilder MapAntiforgeryEndpoints(this RouteGroupBuilder group)
    {
        group.MapModuleGet("/antiforgery", (IAntiforgery antiforgery, HttpContext context) =>
        {
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(context);
            return Results.Ok(new AntiforgeryTokenDto(tokens.RequestToken, "X-XSRF-TOKEN"));
        }, "Authentication_Antiforgery_Get", typeof(AntiforgeryTokenDto))
        .Produces<AntiforgeryTokenDto>(StatusCodes.Status200OK)
        .WithSummary("Gets an antiforgery request token.");

        return group;
    }
}
