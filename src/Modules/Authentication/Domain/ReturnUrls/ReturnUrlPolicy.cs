namespace Vitarish.Authentication.Domain.ReturnUrls;

public static class ReturnUrlPolicy
{
    public static bool IsSafeLocal(string? returnUrl) => string.IsNullOrWhiteSpace(returnUrl) || (returnUrl.StartsWith("/", StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal) && !returnUrl.StartsWith("/\\", StringComparison.Ordinal));
    public static ReturnUrl NormalizeOrDefault(string? returnUrl, string fallback = "/") => IsSafeLocal(returnUrl) ? new(string.IsNullOrWhiteSpace(returnUrl) ? fallback : returnUrl) : throw new ArgumentException("Return URL must be local.", nameof(returnUrl));
}
