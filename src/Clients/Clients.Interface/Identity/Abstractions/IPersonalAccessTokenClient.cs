namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IPersonalAccessTokenClient
{
    Task<string?> CreateTokenAsync(
        string userId,
        int lifetimeDays = 30,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}
