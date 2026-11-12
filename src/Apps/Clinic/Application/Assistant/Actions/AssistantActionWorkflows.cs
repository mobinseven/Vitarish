using MediatR;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.PreparedActions;
using Vitarish.Intelligence.Domain.PreparedActions;

namespace Vitarish.Clinic.Application.Assistant.Actions;

public enum AssistantActionOutcome
{
    Succeeded,
    ValidationFailure,
    NotFound,
    ScopeMismatch,
    PermissionDenied,
    Expired,
    Conflict,
    Failed
}

public sealed record AssistantActionResult(AssistantActionOutcome Outcome, AssistantAction? Action = null);

public sealed record ConfirmAssistantAction(Guid ActionId, AssistantContext Context)
    : IRequest<AssistantActionResult>;

public sealed class ConfirmAssistantActionHandler(
    PreparedActionLifecycle lifecycle,
    IAssistantActionCommitterRegistry committers,
    AssistantCapabilityRegistry capabilityRegistry) : IRequestHandler<ConfirmAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(ConfirmAssistantAction request, CancellationToken cancellationToken) =>
        ClinicPreparedActionMapping.ToClinic(await lifecycle.ConfirmAsync(
            request.ActionId,
            new ClinicPreparedActionApp(request.Context, capabilityRegistry, committers),
            cancellationToken));
}

public sealed record RejectAssistantAction(Guid ActionId, AssistantContext Context, string? Reason)
    : IRequest<AssistantActionResult>;

public sealed class RejectAssistantActionHandler(
    PreparedActionLifecycle lifecycle,
    IAssistantActionCommitterRegistry committers,
    AssistantCapabilityRegistry capabilityRegistry) : IRequestHandler<RejectAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(RejectAssistantAction request, CancellationToken cancellationToken) =>
        ClinicPreparedActionMapping.ToClinic(await lifecycle.RejectAsync(
            request.ActionId,
            new ClinicPreparedActionApp(request.Context, capabilityRegistry, committers),
            request.Reason,
            cancellationToken));
}

public sealed record GetAssistantAction(Guid ActionId, AssistantContext Context)
    : IRequest<AssistantActionResult>;

public sealed class GetAssistantActionHandler(
    PreparedActionLifecycle lifecycle,
    IAssistantActionCommitterRegistry committers,
    AssistantCapabilityRegistry capabilityRegistry) : IRequestHandler<GetAssistantAction, AssistantActionResult>
{
    public async Task<AssistantActionResult> Handle(GetAssistantAction request, CancellationToken cancellationToken) =>
        ClinicPreparedActionMapping.ToClinic(await lifecycle.GetAsync(
            request.ActionId,
            new ClinicPreparedActionApp(request.Context, capabilityRegistry, committers),
            cancellationToken));
}

public sealed record ReviseAssistantAction(Guid ActionId, AssistantContext Context, string ArgumentsJson)
    : IRequest<AssistantActionResult>;

/// <summary>
/// Replaces a proposal with the owner's edit in the same run. Only revisable action policies allow it; the edit
/// re-runs the original tool's prepare step under the request's guards, must keep the original operation, and is
/// reviewed and confirmed like any proposal.
/// </summary>
public sealed class ReviseAssistantActionHandler(
    PreparedActionLifecycle lifecycle,
    IAssistantActionCommitterRegistry committers,
    AssistantCapabilityRegistry capabilityRegistry,
    IAssistantActionPreparation preparation,
    TimeProvider timeProvider) : IRequestHandler<ReviseAssistantAction, AssistantActionResult>
{
    private const string OperationProperty = "operation";

    public async Task<AssistantActionResult> Handle(ReviseAssistantAction request, CancellationToken cancellationToken) =>
        ClinicPreparedActionMapping.ToClinic(await lifecycle.ReviseAsync(
            request.ActionId,
            new ClinicPreparedActionApp(request.Context, capabilityRegistry, committers),
            (original, ct) => PrepareReplacementAsync(request, original, ct),
            cancellationToken));

    private async Task<PreparedAction?> PrepareReplacementAsync(
        ReviseAssistantAction request,
        PreparedAction original,
        CancellationToken cancellationToken)
    {
        AssistantActionPolicy? policy = capabilityRegistry.ResolveAction(original.ActionType);
        if (policy is not { Revisable: true, RequiresConfirmation: true } || original.ToolName is not { } toolName ||
            !policy.ToolNames.Contains(toolName) || !KeepsOperation(original.ReviewPayload, request.ArgumentsJson))
            return null;

        AssistantPreparedAction? prepared = await preparation.PrepareAsync(request.Context, toolName, request.ArgumentsJson, cancellationToken);
        if (prepared is null ||
            !string.Equals(prepared.ActionType, original.ActionType, StringComparison.Ordinal) ||
            !string.Equals(prepared.Capability, original.Capability, StringComparison.Ordinal))
            return null;

        DateTimeOffset preparedAt = timeProvider.GetUtcNow();
        return ClinicPreparedActionMapping.ToPrepared(
            request.Context,
            prepared with { Summary = original.Summary, ToolCallId = prepared.OwningId.ToString("N"), ToolName = toolName },
            original.AgentRunId,
            original.ConversationId,
            preparedAt,
            prepared.Expiration ?? preparedAt.Add(policy.DefaultProposalLifetime ?? TimeSpan.FromMinutes(30)),
            policy.Revision);
    }

    // An edit changes the details of the proposed operation, never which operation it is.
    private static bool KeepsOperation(string originalPayload, string revisedArguments)
    {
        try
        {
            using System.Text.Json.JsonDocument original = System.Text.Json.JsonDocument.Parse(originalPayload);
            using System.Text.Json.JsonDocument revised = System.Text.Json.JsonDocument.Parse(revisedArguments);
            if (revised.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return false;
            if (original.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object ||
                !original.RootElement.TryGetProperty(OperationProperty, out System.Text.Json.JsonElement before))
                return true;
            return revised.RootElement.TryGetProperty(OperationProperty, out System.Text.Json.JsonElement after) &&
                before.ValueKind == System.Text.Json.JsonValueKind.String &&
                after.ValueKind == System.Text.Json.JsonValueKind.String &&
                string.Equals(before.GetString(), after.GetString(), StringComparison.Ordinal);
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
