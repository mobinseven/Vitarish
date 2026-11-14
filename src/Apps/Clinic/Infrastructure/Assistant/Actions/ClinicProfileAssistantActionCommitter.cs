using System.Text.Json;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;

namespace Vitarish.Clinic.Infrastructure.Assistant.Actions;

/// <summary>
/// Commits a confirmed clinic-profile change in the confirming request's context. It applies the change onto the
/// profile as it is now, so parts saved since the review (working hours, photos) are kept, then saves through the
/// Parties profile command, which validates again.
/// </summary>
public sealed class ClinicProfileAssistantActionCommitter(ISender sender) : IAssistantActionCommitter
{
    private const string CompletionPayload = "{}";

    public string ActionType => ClinicSetupAssistant.ProfileActionType;
    public string WorkflowKey => ClinicSetupAssistant.ProfileWorkflowKey;

    public async Task<AssistantActionCommitResult> CommitAsync(
        AssistantAction action,
        AssistantContext context,
        CancellationToken cancellationToken)
    {
        ClinicProfileReview? review;
        try
        {
            review = JsonSerializer.Deserialize(action.ReviewPayload, ClinicProfileJsonSerializerContext.Default.ClinicProfileReview);
        }
        catch (JsonException)
        {
            review = null;
        }

        if (review is null)
            return AssistantActionCommitResult.Failed(ClinicSetupAssistant.ProfileFailureCodes.InvalidReview, "The clinic profile review is invalid.");

        TenantBusinessProfileView? current = await sender.Send(new GetTenantBusinessProfileQuery(), cancellationToken);
        if (current is null)
            return AssistantActionCommitResult.Failed(ClinicSetupAssistant.ProfileFailureCodes.NotFound, "The clinic was not found.");
        if (!ClinicProfileChange.TryApply(current, review, out string? description, out TenantBusinessDetails? details, out string? problem))
            return AssistantActionCommitResult.Failed(ClinicSetupAssistant.ProfileFailureCodes.Invalid, problem ?? "The clinic profile change is not valid.");

        TenantBusinessProfileUpdateResult result = await sender.Send(
            new UpdateTenantBusinessProfileCommand(description, current.Category, details), cancellationToken);
        return result.Status switch
        {
            TenantBusinessProfileUpdateStatus.Updated => AssistantActionCommitResult.Completed(CompletionPayload),
            TenantBusinessProfileUpdateStatus.NotFound => AssistantActionCommitResult.Failed(
                ClinicSetupAssistant.ProfileFailureCodes.NotFound, "The clinic was not found."),
            _ => AssistantActionCommitResult.Failed(
                ClinicSetupAssistant.ProfileFailureCodes.Invalid, result.Error ?? "The clinic profile change is not valid.")
        };
    }
}
