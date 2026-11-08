namespace Vitarish.Authentication.Contracts.Requests;

public sealed record BeginAuthenticationContinuationRequest(string? ReturnPath);
public sealed record CompleteAuthenticationContinuationRequest(string Token);
