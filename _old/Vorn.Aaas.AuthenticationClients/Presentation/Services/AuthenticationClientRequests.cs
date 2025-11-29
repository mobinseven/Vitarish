namespace Vorn.Aaas.AuthenticationClients.Presentation.Services;

/// <summary>
/// Request payload used when creating a new AuthenticationClient.
/// </summary>
public sealed record CreateAuthenticationClientRequest(
    string Id,
    string Url,
    string? Title,
    string? Description,
    string? LogoUrl,
    bool AutoAccess);

/// <summary>
/// Request payload used when updating an existing AuthenticationClient.
/// </summary>
public sealed record UpdateAuthenticationClientRequest(
    string Id,
    string Url,
    string? Title,
    string? Description,
    string? LogoUrl,
    bool AutoAccess);

/// <summary>
/// Request payload used when deleting an AuthenticationClient.
/// </summary>
public sealed record DeleteAuthenticationClientRequest(string Id);
