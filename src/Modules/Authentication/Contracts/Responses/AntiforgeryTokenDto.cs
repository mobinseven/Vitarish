namespace Vitarish.Authentication.Contracts.Responses;

public sealed record AntiforgeryTokenDto(string? RequestToken, string HeaderName);
