namespace Vitarish.Users.Contracts.Responses;

public sealed record PatCreateResponse(string Token);
public sealed record PatListResponse(IReadOnlyCollection<string> Tokens);
public sealed record WebAuthnCredentialResponse(Guid Id, string? FriendlyName, DateTime RegisteredAt, DateTime? LastUsedAt, string? Transports);
