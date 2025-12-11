namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IPersonalAccessTokenClient
{
    Task<string?> CreateTokenAsync(
        int? lifetimeDays = null,
        IEnumerable<string>? scopes = null,
        bool isReferenceToken = false,
        CancellationToken cancellationToken = default);
}
