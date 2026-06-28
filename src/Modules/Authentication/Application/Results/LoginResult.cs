namespace Vitarish.Authentication.Application.Results;

public sealed record LoginResult(string UserId, string ReturnUrl, bool RequiresProfileCompletion);
