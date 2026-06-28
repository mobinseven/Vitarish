namespace Vitarish.Authentication.Contracts.Responses;

public sealed record LogoutContextDto(string? LogoutId, string? ClientName, string? PostLogoutRedirectUri, bool ShowLogoutPrompt);
