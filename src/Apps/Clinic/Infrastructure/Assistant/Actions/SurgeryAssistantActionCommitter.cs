using System.Text.Json;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Surgeries;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Surgery;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>
/// Applies a confirmed surgery-workflow proposal through the same owners the surgery API uses, which enforce
/// owner-only planning, status rules, and stale-write checks. The proposal must belong to the confirming scope.
/// </summary>
public sealed class SurgeryAssistantActionCommitter(
    ISurgeryAssistantProposalStore proposals,
    ISurgeryToolchain surgeries,
    ISurgeryCatalogStore catalogs,
    ISurgerySettingsStore settings) : IAssistantActionCommitter
{
    private const string SurgeryChanged = "surgery_changed";

    public string ActionType => SurgeryAssistantActions.Create;
    public string WorkflowKey => SurgeryAssistantActions.Workflow;
    public IReadOnlySet<string> ActionTypes => SurgeryAssistantActions.ActionTypes;

    public async Task<AssistantActionCommitResult> CommitAsync(AssistantAction action, AssistantContext context, CancellationToken cancellationToken)
    {
        SurgeryAssistantProposal? proposal = await proposals.GetAsync(action.OwningProposalId, cancellationToken);
        if (proposal is null)
            return AssistantActionCommitResult.Failed("owning_proposal_not_found", "The surgery proposal was not found; prepare it again.");
        if (!BelongsTo(proposal, action, context))
            return AssistantActionCommitResult.Failed("owning_proposal_forbidden", "The surgery proposal does not belong to this scope.");

        try
        {
            Guid? entityId = await ApplyAsync(proposal, cancellationToken);
            return entityId is { } id
                ? AssistantActionCommitResult.Completed(JsonSerializer.Serialize(new { entityId = id.ToString("D") }))
                : AssistantActionCommitResult.Failed(SurgeryChanged, "The surgery or setting no longer exists or changed since the proposal; prepare it again.");
        }
        catch (ArgumentException exception)
        {
            return AssistantActionCommitResult.Failed("invalid_surgery", exception.Message);
        }
        catch (SurgeryPlanningForbiddenException exception)
        {
            return AssistantActionCommitResult.Failed("surgery_forbidden", exception.Message);
        }
        catch (SurgeryOperationUnavailableException exception)
        {
            return AssistantActionCommitResult.Failed("surgery_unavailable", exception.Message, true);
        }
        catch (InvalidOperationException exception) when (IsCreation(proposal.ActionType))
        {
            // A creation still holding its idempotency lease can be confirmed again once it settles.
            return AssistantActionCommitResult.Failed("surgery_in_progress", exception.Message, true);
        }
        catch (InvalidOperationException exception)
        {
            return AssistantActionCommitResult.Failed(SurgeryChanged, exception.Message);
        }
    }

    private async Task<Guid?> ApplyAsync(SurgeryAssistantProposal proposal, CancellationToken cancellationToken)
    {
        switch (proposal.ActionType)
        {
            case SurgeryAssistantActions.Create or SurgeryAssistantActions.OperationsCreate:
                return (await surgeries.CreateAsync(Required(proposal.Surgery), cancellationToken)).Id;
            case SurgeryAssistantActions.Update or SurgeryAssistantActions.OperationsUpdate:
                return (await surgeries.UpdateAsync(Required(proposal.Surgery), cancellationToken))?.Id;
            case SurgeryAssistantActions.Complete or SurgeryAssistantActions.OperationsComplete:
                return (await surgeries.CompleteAsync(Required(proposal.SurgeryId), proposal.ExpectedConcurrencyToken, cancellationToken))?.Id;
            case SurgeryAssistantActions.Cancel or SurgeryAssistantActions.OperationsCancel:
                return (await surgeries.CancelAsync(Required(proposal.SurgeryId), proposal.ExpectedConcurrencyToken, cancellationToken))?.Id;
            case SurgeryAssistantActions.CreateType:
                return Guid.TryParse((await catalogs.CreateAsync(Required(proposal.SurgeryType), cancellationToken)).Id, out Guid created) ? created : null;
            case SurgeryAssistantActions.UpdateType:
                Guid typeId = Required(proposal.SurgeryTypeId);
                return await catalogs.UpdateAsync(typeId, Required(proposal.SurgeryType), proposal.ExpectedConcurrencyToken, cancellationToken) is null ? null : typeId;
            case SurgeryAssistantActions.DeleteType:
                Guid deletedId = Required(proposal.SurgeryTypeId);
                return await catalogs.DeleteAsync(deletedId, proposal.ExpectedConcurrencyToken, cancellationToken) ? deletedId : null;
            case SurgeryAssistantActions.UpdateCancellationMessage:
                return await settings.UpdateAsync(Required(proposal.CancellationMessage), proposal.ExpectedConcurrencyToken, cancellationToken) is null
                    ? null
                    : proposal.Id;
            default:
                throw new ArgumentException("The surgery action is not supported.");
        }
    }

    private static bool BelongsTo(SurgeryAssistantProposal proposal, AssistantAction action, AssistantContext context) =>
        proposal.Id == action.OwningProposalId &&
        string.Equals(proposal.ActionType, action.ActionType, StringComparison.Ordinal) &&
        string.Equals(proposal.PracticeId, action.PracticeId, StringComparison.Ordinal) &&
        string.Equals(proposal.PracticeId, context.PracticeId, StringComparison.Ordinal) &&
        string.Equals(proposal.RequestedByUserId, context.AuthenticatedUserId, StringComparison.Ordinal) &&
        string.Equals(proposal.SubjectKind, context.SubjectKind, StringComparison.Ordinal) &&
        proposal.SubjectPatientId == (context.SubjectKind == SurgeryAssistantScope.PatientSubject ? context.PatientId : Guid.Empty);

    private static bool IsCreation(string actionType) =>
        actionType is SurgeryAssistantActions.Create or SurgeryAssistantActions.OperationsCreate;

    private static T Required<T>(T? value) where T : class =>
        value ?? throw new ArgumentException("The surgery proposal is incomplete.");

    private static Guid Required(Guid? value) =>
        value ?? throw new ArgumentException("The surgery proposal is incomplete.");
}
