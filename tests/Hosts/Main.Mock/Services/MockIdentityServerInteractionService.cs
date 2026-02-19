using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace Vitarish.Main.Mock.Services;

public sealed class MockIdentityServerInteractionService : IIdentityServerInteractionService
{
    public Task<AuthorizationRequest?> GetAuthorizationContextAsync(string? returnUrl) => Task.FromResult<AuthorizationRequest?>(null);

    public Task<LogoutRequest> GetLogoutContextAsync(string? logoutId) => Task.FromResult(new LogoutRequest("mock-iframe-url", new LogoutMessage()));

    public Task<string?> CreateLogoutContextAsync() => Task.FromResult<string?>(Guid.NewGuid().ToString());

    public Task GrantConsentAsync(AuthorizationRequest request, ConsentResponse consent, string? subject = null) => Task.CompletedTask;

    public Task DenyAuthorizationAsync(AuthorizationRequest request, AuthorizationError error, string? errorDescription = null) => Task.CompletedTask;

    public bool IsValidReturnUrl(string? returnUrl) => true;

    public bool IsValidLogoutRedirectUri(string? url) => true;

    public Task<ConsentResponse?> GetUserConsentAsync(AuthorizationRequest request) => Task.FromResult<ConsentResponse?>(new ConsentResponse());

    public Task<ErrorMessage?> GetErrorContextAsync(string? errorId) => Task.FromResult<ErrorMessage?>(null);

    public Task<IEnumerable<Grant>> GetAllUserGrantsAsync() => Task.FromResult<IEnumerable<Grant>>(Array.Empty<Grant>());

    public Task RevokeUserConsentAsync(string? clientId) => Task.CompletedTask;

    public Task RevokeTokensForCurrentSessionAsync() => Task.CompletedTask;
}
