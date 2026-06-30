namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationRedirectResolver
{
    string Resolve(string? requestedReturnUrl, bool requiresProfileCompletion);
    Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, CancellationToken cancellationToken = default);
    Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, string? userId, CancellationToken cancellationToken = default);
}
