namespace Vitarish.Intelligence.Application.Agents;

/// <summary>The sequential, non-overlapping phases of one conversation turn inside the workflow.</summary>
internal enum AgentTurnPhase
{
    /// <summary>Lease acquisition together with the stored history load.</summary>
    LeaseAcquire,
    /// <summary>Execution context preparation and any history carry-over.</summary>
    HistoryPreparation,
    /// <summary>App-owned validation of the runtime result before it is committed.</summary>
    OutputValidation,
    /// <summary>Atomic history and proposal persistence.</summary>
    Persistence,
    /// <summary>Lease release and cleanup after the result is decided.</summary>
    LeaseRelease
}
