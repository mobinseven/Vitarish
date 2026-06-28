namespace Vitarish.Authentication.Contracts.Requests;

public sealed record BeginEmailLoginRequest(string Email, string? ReturnUrl);
