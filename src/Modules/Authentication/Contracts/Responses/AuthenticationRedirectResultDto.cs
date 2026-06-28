namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AuthenticationRedirectResultDto(string ReturnUrl, bool RequiresProfileCompletion);
