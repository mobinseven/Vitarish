namespace Vitarish.Base.Domain.Models;

/// <summary>
/// Base class for entity descriptor DTOs - lightweight representations of entity DTOs.
/// </summary>
public record EntityDescriptorDto
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }
}
