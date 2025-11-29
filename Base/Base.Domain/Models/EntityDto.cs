namespace Vitarish.Base.Domain.Models;

/// <summary>
/// Base class for entity DTOs (Data Transfer Objects).
/// </summary>
public record EntityDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
