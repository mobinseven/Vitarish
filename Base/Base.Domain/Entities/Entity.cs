namespace Vitarish.Base.Domain.Entities;

/// <summary>
/// Base class for all domain entities with a GUID identifier.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this entity is deleted (soft delete).
    /// </summary>
    public bool IsDeleted { get; set; }
}
