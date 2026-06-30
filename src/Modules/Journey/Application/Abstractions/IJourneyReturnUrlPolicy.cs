namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Centralizes return URL validation and normalization.
/// </summary>
public interface IJourneyReturnUrlPolicy
{
    /// <summary>
    /// Determines if the return URL is safe for use.
    /// </summary>
    bool IsSafe(string? returnUrl);

    /// <summary>
    /// Normalizes the return URL or returns null if unsafe.
    /// </summary>
    string? Normalize(string? returnUrl);
}
