namespace Vitarish.Authentication.Application.Abstractions;

public interface ILogoutContextService { Task<LogoutContext?> GetAsync(string? logoutId, CancellationToken cancellationToken); }
public sealed record LogoutContext(string? LogoutId, string? ClientName, string? PostLogoutRedirectUri, bool ShowLogoutPrompt);
