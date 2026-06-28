namespace Vitarish.Authentication.Application.Results;

public abstract record AuthenticationResult<T> { public sealed record Success(T Value) : AuthenticationResult<T>; public sealed record Failure(AuthenticationFailure Error) : AuthenticationResult<T>; }
