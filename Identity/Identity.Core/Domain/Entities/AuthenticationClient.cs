using System.ComponentModel.DataAnnotations;

namespace Vitarish.Identity.Core.Domain.Entities;

/// <summary>
/// Represents a frontend authentication client application.
/// Used to configure allowed client applications for authentication.
/// </summary>
public class AuthenticationClient
{
    /// <summary>
    /// Gets or sets the unique identifier for the client.
    /// </summary>
    [Key]
    [Required]
    [MaxLength(128)]
    public required string Id { get; set; }

    /// <summary>
    /// Gets or sets the base URL of the client application.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the display title of the client.
    /// </summary>
    [MaxLength(256)]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets an optional description of the client.
    /// </summary>
    [MaxLength(1024)]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL of the client's logo.
    /// </summary>
    [MaxLength(512)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the client is allowed immediate access without manual approval.
    /// </summary>
    public bool AutoAccess { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when this client was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this client was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
}
