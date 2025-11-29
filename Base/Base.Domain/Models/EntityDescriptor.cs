namespace Vitarish.Base.Domain.Models;

/// <summary>
/// Base class for entity descriptors - lightweight representations of entities.
/// </summary>
public record EntityDescriptor
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public Guid Id { get; set; }
}
