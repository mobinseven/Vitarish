namespace Vitarish.Clinic.Application.PatientAgent.Actions;

public interface IPatientAgentActionStore
{
    Task SaveAsync(PatientAgentAction action, CancellationToken cancellationToken);
    Task<PatientAgentAction?> GetAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken);
    Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken);
    Task<bool> FailAsync(Guid actionId, PatientAgentActionFailure failure, CancellationToken cancellationToken);
    Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken);
}

public interface IPatientAgentActionCommitter
{
    string ActionType { get; }
    Task<PatientAgentActionCommitResult> CommitAsync(
        PatientAgentAction action,
        PatientAgentContext context,
        CancellationToken cancellationToken);
}

public interface IPatientAgentActionCommitterRegistry
{
    IPatientAgentActionCommitter? Resolve(string actionType);
}

public sealed record PatientAgentActionCommitResult(
    bool Succeeded,
    string? CompletionPayload = null,
    PatientAgentActionFailure? Failure = null)
{
    public static PatientAgentActionCommitResult Completed(string payload) => new(true, payload);
    public static PatientAgentActionCommitResult Failed(string code, string message, bool retryable = false) =>
        new(false, Failure: new(code, message, retryable));
}

public sealed class PatientAgentActionCommitterRegistry(IEnumerable<IPatientAgentActionCommitter> committers)
    : IPatientAgentActionCommitterRegistry
{
    private readonly IReadOnlyDictionary<string, IPatientAgentActionCommitter> _committers = committers.ToDictionary(
        item => item.ActionType,
        StringComparer.Ordinal);

    public IPatientAgentActionCommitter? Resolve(string actionType) =>
        _committers.GetValueOrDefault(actionType);
}
