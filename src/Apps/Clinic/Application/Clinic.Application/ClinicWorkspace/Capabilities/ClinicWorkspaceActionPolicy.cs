using Vitarish.Clinic.Application.ClinicWorkspace.Operations;

namespace Vitarish.Clinic.Application.ClinicWorkspace.Capabilities;

public enum ClinicWorkspaceActionExecutionMode
{
    PrepareAndConfirm,
    DirectAllowlisted
}

public enum ClinicWorkspaceActionRetryClassification
{
    NeverRetry,
    Retryable,
    Idempotent
}

/// <summary>Clinic-owned metadata for one executable action and its model-facing tools.</summary>
public sealed record ClinicWorkspaceActionPolicy(
    string ActionType,
    string OwningWorkflow,
    IReadOnlySet<string> RequiredPermissions,
    IReadOnlySet<string> RequiredRoles,
    ClinicWorkspaceActionExecutionMode ExecutionMode,
    int Revision,
    TimeSpan? DefaultProposalLifetime,
    ClinicWorkspaceActionRetryClassification RetryClassification,
    IReadOnlySet<string> ToolNames)
{
    public bool RequiresConfirmation => ExecutionMode == ClinicWorkspaceActionExecutionMode.PrepareAndConfirm;

    public bool Authorizes(ClinicWorkspaceContext context)
    {
        bool permissionsSatisfied = string.Equals(context.Role, ClinicWorkspaceOperationsScope.PlatformAdminRole, StringComparison.Ordinal) ||
            RequiredPermissions.All(context.Permissions.Contains);
        bool rolesSatisfied = RequiredRoles.Count == 0 ||
            RequiredRoles.Contains(context.Role) ||
            string.Equals(context.Role, Operations.ClinicWorkspaceOperationsScope.PlatformAdminRole, StringComparison.Ordinal);
        return permissionsSatisfied && rolesSatisfied;
    }
}

public static class ClinicWorkspaceActionPolicies
{
    public static ClinicWorkspaceActionPolicy Prepare(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles = null,
        IEnumerable<string>? toolNames = null,
        TimeSpan? defaultProposalLifetime = null,
        int revision = 1,
        ClinicWorkspaceActionRetryClassification retryClassification = ClinicWorkspaceActionRetryClassification.NeverRetry) =>
        Create(actionType, owningWorkflow, requiredPermissions, requiredRoles, ClinicWorkspaceActionExecutionMode.PrepareAndConfirm,
            defaultProposalLifetime ?? TimeSpan.FromMinutes(30), retryClassification, toolNames, revision);

    public static ClinicWorkspaceActionPolicy Direct(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles = null,
        IEnumerable<string>? toolNames = null,
        int revision = 1) =>
        Create(actionType, owningWorkflow, requiredPermissions, requiredRoles, ClinicWorkspaceActionExecutionMode.DirectAllowlisted,
            null, ClinicWorkspaceActionRetryClassification.Idempotent, toolNames, revision);

    private static ClinicWorkspaceActionPolicy Create(
        string actionType,
        string owningWorkflow,
        IEnumerable<string> requiredPermissions,
        IEnumerable<string>? requiredRoles,
        ClinicWorkspaceActionExecutionMode executionMode,
        TimeSpan? defaultProposalLifetime,
        ClinicWorkspaceActionRetryClassification retryClassification,
        IEnumerable<string>? toolNames,
        int revision)
    {
        if (string.IsNullOrWhiteSpace(actionType) || string.IsNullOrWhiteSpace(owningWorkflow) || revision < 1)
            throw new ArgumentException("An action policy requires an action type, workflow owner, and positive revision.");
        if (executionMode == ClinicWorkspaceActionExecutionMode.PrepareAndConfirm &&
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
