namespace Vitarish.Base.Domain.Primitives;

/// <summary>
/// Base class for all entities in the domain.
/// Entities have identity and are compared by their Id.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Gets or sets the timestamp when this entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when this entity was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this entity was soft-deleted.
    /// Null means the entity is not deleted.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// Gets the domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event for this entity.
    /// </summary>
    /// <param name="domainEvent">The domain event to raise.</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events from this entity.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Marks this entity as soft-deleted.
    /// </summary>
    public virtual void MarkAsDeleted()
    {
        DeletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this entity as updated.
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Entity<TId>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
