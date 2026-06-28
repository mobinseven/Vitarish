using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Api.Mapping;
using Vitarish.Authentication.Contracts.Requests;
using Vitarish.Authentication.Contracts.Responses;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AccountPhoneChangeEndpoints
{
    public static IEndpointRouteBuilder MapAccountPhoneChangeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/account/phone/begin", async (BeginChangePhoneRequest request, BeginChangePhoneHandler handler, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, CancellationToken ct) =>
        {
            string? userId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new BeginChangePhoneCommand(userId, request.PhoneNumber), ct);
            return r is AuthenticationResult<ChallengeResult>.Success s
                ? Results.Ok(AuthenticationEndpointMapper.ToDto(s.Value))
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<ChallengeResult>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Account_Phone_Change_Begin")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpBegin)
        .Produces<AuthenticationChallengeDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Begins phone number change.");

        endpoints.MapPost("/account/phone/complete", async (CompleteChangePhoneRequest request, CompleteChangePhoneHandler handler, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor, CancellationToken ct) =>
        {
            string? userId = httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

            var r = await handler.Handle(new CompleteChangePhoneCommand(userId, request.ChallengeId, request.Code), ct);
            return r is AuthenticationResult<bool>.Success s && s.Value
                ? Results.Ok(new { Succeeded = true })
                : AuthenticationProblemHttpMapper.ToProblem(((AuthenticationResult<bool>.Failure)r).Error);
        })
        .RequireAuthorization()
        .WithName("Authentication_Account_Phone_Change_Complete")
        .RequireRateLimiting(AuthenticationRateLimitPolicies.OtpComplete)
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status410Gone)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .WithSummary("Completes phone number change.");

        return endpoints;
    }
}
