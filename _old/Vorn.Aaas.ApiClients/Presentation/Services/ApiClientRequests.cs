namespace Vorn.Aaas.ApiClients.Presentation.Services;

/// <summary>
/// Request payload used when creating a new ApiClient.
/// </summary>
public sealed record CreateApiClientRequest(
    string Id,
    string Secret,
    string Scopes,
    string HostUri);

/// <summary>
/// Request payload used when updating an existing ApiClient.
/// </summary>
public sealed record UpdateApiClientRequest(
    string Id,
    string Secret,
    string Scopes,
    string HostUri);

/// <summary>
/// Request payload used when deleting an ApiClient.
/// </summary>
public sealed record DeleteApiClientRequest(string Id);
