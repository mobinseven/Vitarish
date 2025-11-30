using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Presentation.Contracts;

namespace Vorn.Aaas.ApiClients.Presentation.Abstractions;
/// <summary>
/// Contract for working with ApiClients inside the application (no HTTP required).
/// </summary>
public interface IApiClientsService
{
    Task<ApiClient?> GetApiClientAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<ApiClient>> GetApiClientsAsync(CancellationToken ct = default);
    /// <summary>
    /// Creates a new client and returns its assigned id (matches the CreatedLocation in the HTTP API).
    /// </summary>
    Task<ApiClient?> CreateApiClientAsync(CreateApiClientRequest request, CancellationToken ct = default);
    /// <summary>
    /// Edits an existing client and returns the updated entity (or null if not found).
    /// </summary>
    Task<ApiClient?> UpdateApiClientAsync(UpdateApiClientRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a client by identifier.
    /// </summary>
    Task DeleteApiClientAsync(string id, CancellationToken ct = default);
}

