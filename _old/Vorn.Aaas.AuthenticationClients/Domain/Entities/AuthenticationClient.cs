namespace Vorn.Aaas.AuthenticationClients.Domain.Entities;

/// <summary>
/// Represents a frontend authentication client application.
/// </summary>
public class AuthenticationClient
{
    /// <summary>Unique identifier for the client.</summary>
    public required string Id { get; set; }

    /// <summary>Base URL of the client.</summary>
    public required string Url { get; set; }

    /// <summary>Display title.</summary>
    public string? Title { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>URL of the logo.</summary>
    public string? LogoUrl { get; set; }

    /// <summary>Whether the client is allowed immediate access.</summary>
    public bool AutoAccess { get; set; } = true;
}
