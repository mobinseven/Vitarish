namespace Vitarish.Clinic.Application.Assistant.Actions;

public interface IAssistantActionStore
{
    Task SaveAsync(AssistantAction action, CancellationToken cancellationToken);
    Task<AssistantAction?> GetAsync(Guid actionId, CancellationToken cancellationToken);
    Task<bool> TryBeginConfirmationAsync(Guid actionId, bool retryFailed, CancellationToken cancellationToken);
    Task<bool> TryRejectAsync(Guid actionId, DateTimeOffset rejectedAt, string? reason, CancellationToken cancellationToken);
    Task<bool> CompleteAsync(Guid actionId, DateTimeOffset confirmedAt, string confirmedByUserId, string completionPayload, CancellationToken cancellationToken);
    Task<bool> FailAsync(Guid actionId, AssistantActionFailure failure, CancellationToken cancellationToken);
    Task<bool> ExpireAsync(Guid actionId, CancellationToken cancellationToken);
}

public interface IAssistantActionCommitter
{
    string ActionType { get; }
    string WorkflowKey => ActionType;
    IReadOnlySet<string> ActionTypes => new HashSet<string>(StringComparer.Ordinal) { ActionType };
    Task<AssistantActionCommitResult> CommitAsync(
        AssistantAction action,
        AssistantContext context,
        CancellationToken cancellationToken);
}

public interface IAssistantActionCommitterRegistry
{
    IAssistantActionCommitter? Resolve(string actionType);
}

public sealed record AssistantActionCommitResult(
    bool Succeeded,
    string? CompletionPayload = null,
    AssistantActionFailure? Failure = null)
{
    public static AssistantActionCommitResult Completed(string payload) => new(true, payload);
    public static AssistantActionCommitResult Failed(string code, string message, bool retryable = false) =>
        new(false, Failure: new(code, message, retryable));
}

public sealed class AssistantActionCommitterRegistry(IEnumerable<IAssistantActionCommitter> committers)
    : IAssistantActionCommitterRegistry
{
    private readonly IReadOnlyDictionary<string, IAssistantActionCommitter> _committers = committers
        .SelectMany(item => item.ActionTypes.Select(actionType => (actionType, item)))
        .ToDictionary(item => item.actionType, item => item.item, StringComparer.Ordinal);

    public IAssistantActionCommitter? Resolve(string actionType) =>
        _committers.GetValueOrDefault(actionType);
}
