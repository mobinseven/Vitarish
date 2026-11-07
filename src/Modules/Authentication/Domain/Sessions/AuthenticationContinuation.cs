namespace Vitarish.Authentication.Domain.Sessions;

/// <summary>Browser-correlated request to return from Auth to a registered application.</summary>
public sealed class AuthenticationContinuation
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    public const int MaximumBrowserContinuations = 16;
    public const int MaximumReturnPathLength = 2048;
    public string TokenHash { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string ReturnPath { get; set; } = string.Empty;
    public string CorrelationHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
