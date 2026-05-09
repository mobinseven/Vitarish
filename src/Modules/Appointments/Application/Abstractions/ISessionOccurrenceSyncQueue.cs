namespace Vitarish.Appointments.Application.Abstractions;

/// <summary>
/// Enqueues an immediate background job to refresh occurrence projections for a session.
/// </summary>
public interface ISessionOccurrenceSyncQueue
{
    /// <summary>
    /// Enqueues a fire-and-forget job that calls <see cref="ISessionOccurrenceResolver.SyncOccurrencesAsync"/>
    /// for the specified session in the current tenant's context. Occurrence records are still created lazily when booked.
    /// </summary>
    void EnqueueSync(Guid sessionId);
}
