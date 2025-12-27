using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Vitarish.Main.Mock.Services;

public sealed class MockIdentityServerInteractionService : IIdentityServerInteractionService
{
    public Task<AuthorizationRequest?> GetAuthorizationContextAsync(string? returnUrl)
    {
        return Task.FromResult<AuthorizationRequest?>(null);
    }

    public Task<LogoutRequest> GetLogoutContextAsync(string? logoutId)
    {
        return Task.FromResult(new LogoutRequest("mock-iframe-url", new LogoutMessage()));
    }

    public Task<string?> CreateLogoutContextAsync()
    {
        return Task.FromResult<string?>(Guid.NewGuid().ToString());
    }

    public Task GrantConsentAsync(AuthorizationRequest request, ConsentResponse consent, string? subject = null)
    {
        return Task.CompletedTask;
    }

    public Task DenyAuthorizationAsync(AuthorizationRequest request, AuthorizationError error, string? errorDescription = null)
    {
        return Task.CompletedTask;
    }

    public bool IsValidReturnUrl(string? returnUrl)
    {
        return true;
    }

    public bool IsValidLogoutRedirectUri(string? url)
    {
        return true;
    }

    public Task<ConsentResponse?> GetUserConsentAsync(AuthorizationRequest request)
    {
        return Task.FromResult<ConsentResponse?>(new ConsentResponse());
    }

    public Task<ErrorMessage?> GetErrorContextAsync(string? errorId)
    {
        return Task.FromResult<ErrorMessage?>(null);
    }

    public Task<IEnumerable<Grant>> GetAllUserGrantsAsync()
    {
        return Task.FromResult<IEnumerable<Grant>>(Array.Empty<Grant>());
    }

    public Task RevokeUserConsentAsync(string? clientId)
    {
        return Task.CompletedTask;
    }

    public Task RevokeTokensForCurrentSessionAsync()
    {
        return Task.CompletedTask;
    }
}
