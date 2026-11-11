using MediatR;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Intelligence.Application.PreparedActions;

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
