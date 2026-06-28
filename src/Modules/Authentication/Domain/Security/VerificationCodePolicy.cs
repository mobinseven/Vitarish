namespace Vitarish.Authentication.Domain.Security;

public sealed record VerificationCodePolicy(int RequiredLength) { public bool IsValidFormat(string? code) => !string.IsNullOrWhiteSpace(code) && code.Length == RequiredLength && code.All(char.IsDigit); }
