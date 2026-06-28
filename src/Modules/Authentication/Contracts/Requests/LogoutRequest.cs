namespace Vitarish.Authentication.Contracts.Requests;

public sealed record LogoutRequest(string? LogoutId, string? ReturnUrl);
