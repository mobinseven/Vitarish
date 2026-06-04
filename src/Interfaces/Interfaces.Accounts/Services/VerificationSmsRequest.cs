namespace Vitarish.Interfaces.Accounts.Services;

internal sealed record VerificationSmsRequest(string Key, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
