namespace Vitarish.ApiClients.Domain.Entities;

/// <summary>
/// Represents a backend API client application.
/// </summary>
public class ApiClient
{
    /// <summary>Primary identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Client secret used for token acquisition.</summary>
    public required string Secret { get; set; }

    /// <summary>Allowed scopes for this client.</summary>
    public required string Scopes { get; set; }

    /// <summary>Base URI of the client.</summary>
    public required string HostUri { get; set; }
}
