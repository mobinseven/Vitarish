using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Contracts.Errors;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Api.Mapping;

public static class AuthenticationProblemHttpMapper
{
    public static IResult ToProblem(AuthenticationFailure failure)
    {
        int status = failure.Reason is AuthenticationFailureReason.Unauthorized ? StatusCodes.Status401Unauthorized : StatusCodes.Status400BadRequest;
        return Results.Problem(new ProblemDetails { Status = status, Title = failure.Reason.ToString(), Detail = failure.Message, Extensions = { ["code"] = failure.Reason.ToString() } });
    }
}
