using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountPasswordChangeEndpoints
{
    public static IEndpointRouteBuilder MapAccountPasswordChangeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/password", async (ChangePasswordRequest request, [Microsoft.AspNetCore.Mvc.FromServices] ChangePasswordHandler handler, System.Security.Claims.ClaimsPrincipal user, CancellationToken ct) =>
        {
            string? userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword), ct);
            return r is AuthenticationResult<bool>.Success s && s.Value
                ? Results.Ok(new { Succeeded = true })
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Account_Password_Change")
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .WithSummary("Changes the user password.");

        return endpoints;
    }
}
