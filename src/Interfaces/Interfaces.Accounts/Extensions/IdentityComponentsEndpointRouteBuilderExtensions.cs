
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Vitarish.Interfaces.Accounts.Extensions;

public static class IdentityComponentsEndpointRouteBuilderExtensions
{
    public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder accountGroup = endpoints.MapGroup("/Account");
        accountGroup.WithMetadata("OpenApiExclusionJustification:Razor account endpoints use antiforgery and cookie authentication rather than module contract OpenAPI semantics.").ExcludeFromDescription();

        _ = accountGroup.MapPost("/Logout", async (
            ClaimsPrincipal user,
            SignInManager<User> signInManager,
            [FromForm] string returnUrl) =>
        {
            await signInManager.SignOutAsync();
            return TypedResults.LocalRedirect($"~/{returnUrl}");
        })
        .WithMetadata("OpenApiExclusionJustification:Razor account form endpoint uses antiforgery and cookie authentication rather than module contract OpenAPI semantics.").ExcludeFromDescription();

        return accountGroup;
    }
}
