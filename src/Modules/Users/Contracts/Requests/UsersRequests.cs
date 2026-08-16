namespace Vitarish.Users.Contracts.Requests;

public sealed record CreatePatRequest(int? LifetimeDays = null, IEnumerable<string>? Scopes = null);
public sealed record WebAuthnCompleteRegisterRequest(string AttestationResponseJson);
