namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Client for creating personal access tokens.
/// </summary>
public interface IPersonalAccessTokenClient
{
    /// <summary>
    /// Creates a personal access token for the current user.
    /// </summary>
    /// <param name="lifetimeDays">Token lifetime in days.</param>
    /// <param name="scopes">Requested scopes.</param>
    /// <param name="isReferenceToken">Whether to create a reference token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated token, or null if creation failed.</returns>
    Task<string?> CreateTokenAsync(
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}
