using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class TenantHandoffEndpoints
{
    public static RouteGroupBuilder MapTenantHandoffEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost(AuthenticationRoutes.TenantHandoffRedeemPath, async (
            RedeemTenantHandoffRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] RedeemTenantHandoffHandler handler,
            CancellationToken cancellationToken) =>
        {
            AuthenticationResult<LoginResult> result = await handler.Handle(new RedeemTenantHandoffCommand(request.Token), cancellationToken);
            return result is AuthenticationResult<LoginResult>.Success success
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(success.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)result).Error);
        }, AuthenticationTenantHandoffOperationIds.Redeem, typeof(AuthenticationRedirectResultDto),
            profile: ModuleEndpointProfile.AnonymousJson,
            anonymousJustification: AuthenticationAnonymousEndpointJustifications.TenantHandoff)
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Starts a session on a just-created tenant's host from a one-time handoff ticket.");

        return group;
    }
}
