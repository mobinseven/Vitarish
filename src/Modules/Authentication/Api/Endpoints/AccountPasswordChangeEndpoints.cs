using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountPasswordChangeEndpoints
{
    public static RouteGroupBuilder MapAccountPasswordChangeEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/account/password", async (ChangePasswordRequest request, [Microsoft.AspNetCore.Mvc.FromServices] ChangePasswordHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = AuthenticatedUserIdResolver.Resolve(user) is AuthenticatedUserIdResolution.Success resolvedUserId
                ? resolvedUserId.UserId.Value
                : null;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), ct);
            return r is AuthenticationResult<bool>.Success s && s.Value
                ? Results.Ok(new AuthenticationActionResultDto(true))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)r).Error);
        }, "Authentication_Account_Password_Change", typeof(AuthenticationActionResultDto))
        .WithMetadata(new SecurityAuditHandledByApplicationMetadata())
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Changes the user password.");

        return group;
    }
}
