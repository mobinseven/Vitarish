using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class PasswordLoginOtpEndpoints
{
    public static RouteGroupBuilder MapPasswordLoginOtpEndpoints(this RouteGroupBuilder group)
    {
        group.MapModulePost("/login/password/otp/complete", async (
            CompletePasswordLoginOtpRequest request,
            [Microsoft.AspNetCore.Mvc.FromServices] CompletePasswordLoginOtpHandler handler,
            CancellationToken cancellationToken) =>
        {
            AuthenticationResult<LoginResult> result = await handler.Handle(
                new CompletePasswordLoginOtpCommand(request.ChallengeId, request.Code, request.ReturnUrl),
                cancellationToken);
            return result is AuthenticationResult<LoginResult>.Success success
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(success.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<LoginResult>.Failure)result).Error);
        }, "Authentication_Login_PasswordOtp_Complete", typeof(AuthenticationRedirectResultDto), profile: ModuleEndpointProfile.AnonymousJson, anonymousJustification: "authentication.login.password-otp")
        .WithMetadata(new SecurityAuditRequiredMetadata())
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces<AuthenticationRedirectResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes password plus phone OTP authentication.");

        return group;
    }
}
