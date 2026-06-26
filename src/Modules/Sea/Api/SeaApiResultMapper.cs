using Microsoft.AspNetCore.Http;
using Vitarish.Sea.Contracts.Errors;
using Vitarish.Sea.Contracts.Results;

namespace Vitarish.Sea.Api;

public static class SeaApiResultMapper
{
    public static IResult ToHttpResult<T>(SeaResult<T> result) => result.Succeeded ? Results.Ok(result.Value) : ToHttpError(result.Error!);
    public static IResult ToHttpError(SeaError error) => error.Code switch
    {
        SeaErrorCode.ValidationFailure => Results.BadRequest(error),
        SeaErrorCode.Unauthorized => Results.Unauthorized(),
        SeaErrorCode.Forbidden => Results.Forbid(),
        SeaErrorCode.NotFound => Results.NotFound(error),
        SeaErrorCode.Conflict => Results.Conflict(error),
        SeaErrorCode.Timeout => Results.StatusCode(StatusCodes.Status504GatewayTimeout),
        SeaErrorCode.Cancellation => Results.StatusCode(499),
        _ => Results.Problem(error.Message)
    };
}
