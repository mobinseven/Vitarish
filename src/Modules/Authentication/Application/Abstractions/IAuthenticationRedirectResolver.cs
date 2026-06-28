namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationRedirectResolver { string Resolve(string? requestedReturnUrl, bool requiresProfileCompletion); }
