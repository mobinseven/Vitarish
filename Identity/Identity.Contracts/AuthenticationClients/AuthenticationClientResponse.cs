namespace Vitarish.Identity.Contracts.AuthenticationClients;

/// <summary>
/// Represents an authentication client returned by the API.
/// </summary>
/// <param name="Id">Unique identifier of the authentication client.</param>
/// <param name="Url">Base URL of the authentication client.</param>
/// <param name="Title">Optional display title.</param>
/// <param name="Description">Optional description.</param>
/// <param name="LogoUrl">Optional logo URL.</param>
/// <param name="AutoAccess">Indicates whether the client is granted immediate access.</param>
public sealed record AuthenticationClientResponse(
    string Id,
    string Url,
    string? Title,
    string? Description,
    string? LogoUrl,
    bool AutoAccess);
