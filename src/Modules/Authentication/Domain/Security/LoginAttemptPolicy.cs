namespace Vitarish.Authentication.Domain.Security;

public sealed record LoginAttemptPolicy(int MaximumAttempts) { public bool IsExhausted(int failedAttempts) => failedAttempts >= MaximumAttempts; }
