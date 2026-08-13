using Vitarish.Clinic.Application.Assistant.Operations;

namespace Vitarish.Clinic.Application.Assistant.Capabilities;

public enum AssistantActionExecutionMode
{
    PrepareAndConfirm,
    DirectAllowlisted
}

public enum AssistantActionRetryClassification
{
    NeverRetry,
    Retryable,
    Idempotent
}

/// <summary>Clinic-owned metadata for one executable action and its model-facing tools.</summary>
public sealed record AssistantActionPolicy(
    string ActionType,
    string OwningWorkflow,
    IReadOnlySet<string> RequiredPermissions,
    IReadOnlySet<string> RequiredRoles,
    AssistantActionExecutionMode ExecutionMode,
    int Revision,
    TimeSpan? DefaultProposalLifetime,
    AssistantActionRetryClassification RetryClassification,
    IReadOnlySet<string> ToolNames)
{
    public bool RequiresConfirmation => ExecutionMode == AssistantActionExecutionMode.PrepareAndConfirm;

    public bool Authorizes(AssistantContext context)
    {
        bool permissionsSatisfied = string.Equals(context.Role, AssistantOperationsScope.PlatformAdminRole, StringComparison.Ordinal) ||
            RequiredPermissions.All(context.Permissions.Contains);
        bool rolesSatisfied = RequiredRoles.Count == 0 ||
            RequiredRoles.Contains(context.Role) ||
            string.Equals(context.Role, Operations.AssistantOperationsScope.PlatformAdminRole, StringComparison.Ordinal);
        return permissionsSatisfied && rolesSatisfied;
    }
}

public static class AssistantActionPolicies
{
    public static AssistantActionPolicy Prepare(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles = null,
        IEnumerable<string>? toolNames = null,
        TimeSpan? defaultProposalLifetime = null,
        int revision = 1,
        AssistantActionRetryClassification retryClassification = AssistantActionRetryClassification.NeverRetry) =>
        Create(actionType, owningWorkflow, requiredPermissions, requiredRoles, AssistantActionExecutionMode.PrepareAndConfirm,
            defaultProposalLifetime ?? TimeSpan.FromMinutes(30), retryClassification, toolNames, revision);

    public static AssistantActionPolicy Direct(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles = null,
        IEnumerable<string>? toolNames = null,
        int revision = 1) =>
        Create(actionType, owningWorkflow, requiredPermissions, requiredRoles, AssistantActionExecutionMode.DirectAllowlisted,
            null, AssistantActionRetryClassification.Idempotent, toolNames, revision);

    private static AssistantActionPolicy Create(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles,
        AssistantActionExecutionMode executionMode,
        TimeSpan? defaultProposalLifetime,
        AssistantActionRetryClassification retryClassification,
        IEnumerable<string>? toolNames,
        int revision)
    {
        if (string.IsNullOrWhiteSpace(actionType) || string.IsNullOrWhiteSpace(owningWorkflow) || revision < 1)
            throw new ArgumentException("An action policy requires an action type, workflow owner, and positive revision.");
        if (executionMode == AssistantActionExecutionMode.PrepareAndConfirm &&
            (defaultProposalLifetime is not { } lifetime || lifetime <= TimeSpan.Zero))
            throw new ArgumentException("Prepared actions require a positive default proposal lifetime.", nameof(defaultProposalLifetime));
        return new(
            actionType,
            owningWorkflow,
            requiredPermissions.ToHashSet(StringComparer.Ordinal),
            (requiredRoles ?? []).ToHashSet(StringComparer.Ordinal),
            executionMode,
            revision,
            defaultProposalLifetime,
            retryClassification,
            (toolNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name)).ToHashSet(StringComparer.Ordinal));
    }
}
