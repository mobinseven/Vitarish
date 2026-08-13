namespace Vitarish.Clinic.Application.ClinicWorkspace.Actions;

public interface IClinicWorkspaceActionStore
{
    Task SaveAsync(ClinicWorkspaceAction action, CancellationToken cancellationToken);
    Task<ClinicWorkspaceAction?> GetAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken);
    Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken);
    Task<bool> FailAsync(Guid actionId, ClinicWorkspaceActionFailure failure, CancellationToken cancellationToken);
    Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken);
}

public interface IClinicWorkspaceActionCommitter
{
    string ActionType { get; }
    IReadOnlySet<string> ActionTypes => new HashSet<string>(StringComparer.Ordinal) { ActionType };
    Task<ClinicWorkspaceActionCommitResult> CommitAsync(
        ClinicWorkspaceAction action,
        ClinicWorkspaceContext context,
        CancellationToken cancellationToken);
}

public interface IClinicWorkspaceActionCommitterRegistry
{
    IClinicWorkspaceActionCommitter? Resolve(string actionType);
}

public sealed record ClinicWorkspaceActionCommitResult(
    bool Succeeded,
    string? CompletionPayload = null,
    ClinicWorkspaceActionFailure? Failure = null)
{
    public static ClinicWorkspaceActionCommitResult Completed(string payload) => new(true, payload);
    public static ClinicWorkspaceActionCommitResult Failed(string code, string message, bool retryable = false) =>
        new(false, Failure: new(code, message, retryable));
}

public sealed class ClinicWorkspaceActionCommitterRegistry(IEnumerable<IClinicWorkspaceActionCommitter> committers)
    : IClinicWorkspaceActionCommitterRegistry
{
    private readonly IReadOnlyDictionary<string, IClinicWorkspaceActionCommitter> _committers = committers
        .SelectMany(item => item.ActionTypes.Select(actionType => (actionType, item)))
        .ToDictionary(item => item.actionType, item => item.item, StringComparer.Ordinal);

    public IClinicWorkspaceActionCommitter? Resolve(string actionType) =>
        _committers.GetValueOrDefault(actionType);
}
