namespace Vitarish.Identity.Contracts.Dtos;

/// <summary>
/// Data transfer object for authentication client data.
/// </summary>
public class AuthenticationClientDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the client.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the base URL of the client application.
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the display title of the client.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the client.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL of the client's logo.
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the client is allowed immediate access.
    /// </summary>
    public bool AutoAccess { get; set; }
}
