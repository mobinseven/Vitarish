namespace Vitarish.Authentication.Application.Results;

public sealed record LogoutResult(bool SignedOut, string? RedirectUrl);
