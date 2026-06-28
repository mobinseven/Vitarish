namespace Vitarish.Authentication.Contracts.Responses;

public sealed record LogoutResultDto(bool SignedOut, string? RedirectUrl);
