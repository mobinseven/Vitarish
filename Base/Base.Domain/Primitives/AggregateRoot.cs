namespace Vitarish.Base.Domain.Primitives;

/// <summary>
/// Base class for aggregate roots.
/// An aggregate root is the entry point to an aggregate in DDD.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// Gets or sets the version used for optimistic concurrency control.
    /// </summary>
    public uint Version { get; protected set; }

    /// <summary>
    /// Increments the version number for concurrency tracking.
    /// </summary>
    protected void IncrementVersion()
    {
        Version++;
    }
}
