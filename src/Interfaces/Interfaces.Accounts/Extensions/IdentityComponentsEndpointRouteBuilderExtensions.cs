
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Interfaces.Accounts.Extensions;

public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder accountGroup = endpoints.MapGroup("/Account");
        accountGroup.ExcludeFromOpenApiWithJustification("Razor account endpoints use antiforgery and cookie authentication rather than module contract OpenAPI semantics.");

        _ = accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<User> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        })
        .ExcludeFromOpenApiWithJustification("Razor account form endpoint uses antiforgery and cookie authentication rather than module contract OpenAPI semantics.");

        return accountGroup;
    }
}
