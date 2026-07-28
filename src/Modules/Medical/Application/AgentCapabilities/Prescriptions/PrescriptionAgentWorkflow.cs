using System.Security.Cryptography;
using System.Text;
using MediatR;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.AgentCapabilities.Prescriptions;

public sealed record PrescriptionAgentScope(string TenantId, Guid PatientId, Guid DoctorId, Guid ClinicId, string RequestedByUserId);
public sealed record PrescriptionAgentProvenance(string Origin, string AgentRunId, string ToolInvocationId, string RequestedByUserId, string? ConfirmedByUserId, string TemplateVersion);
public sealed record PrescriptionTemplateMatch(Guid Id, string Name, string? Description, string TemplateVersion);
public enum PrescriptionTemplateSearchStatus { ExactMatch, Ambiguous, NotFound }
public sealed record PrescriptionTemplateSearchResult(PrescriptionTemplateSearchStatus Status, IReadOnlyList<PrescriptionTemplateMatch> Matches);
public sealed record PrescriptionTemplateRequirements(Guid TemplateId, string Name, string TemplateVersion, IReadOnlyList<PrescriptionTemplateItemDto> Items, IReadOnlyList<string> MissingFields);
public enum PrescriptionDraftPreparationStatus { Prepared, MissingInformation, MissingInsuranceCoverage, InvalidTemplate, Failed }
public sealed record PrescriptionDraftReview(Guid ReviewId, Guid PrescriptionId, Guid TemplateId, string TemplateVersion, string InsuranceCoverage, IReadOnlyList<PrescriptionItemDto> Items, PrescriptionAgentProvenance Provenance);
public sealed record PrescriptionDraftPreparationResult(PrescriptionDraftPreparationStatus Status, PrescriptionDraftReview? Review = null, IReadOnlyList<string>? MissingFields = null, string? Message = null);
public enum PrescriptionConfirmationStatus { Submitted, AlreadyConfirmed, ExpiredOrChangedTemplate, NotFound, Forbidden, InvalidDraft, SubmissionFailed }
public sealed record PrescriptionConfirmationResult(PrescriptionConfirmationStatus Status, Guid? PrescriptionId = null, PrescriptionStatus? PrescriptionStatus = null, string? TrackingCode = null, string? Message = null);

public interface IPrescriptionAgentReviewStore
{
    Task<PrescriptionDraftReview?> GetAsync(Guid reviewId, CancellationToken cancellationToken);
    Task SaveAsync(PrescriptionDraftReview review, CancellationToken cancellationToken);
    Task<PrescriptionConfirmationResult?> GetConfirmationAsync(Guid reviewId, CancellationToken cancellationToken);
    Task SaveConfirmationAsync(Guid reviewId, PrescriptionConfirmationResult result, CancellationToken cancellationToken);
}

public sealed class SearchPrescriptionTemplatesForAgent(ISender sender)
{
    public async Task<PrescriptionTemplateSearchResult> ExecuteAsync(string tenantId, string requestedName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(requestedName)) return new(PrescriptionTemplateSearchStatus.NotFound, []);
        IReadOnlyList<PrescriptionTemplateSummaryDto> templates = await sender.Send(new GetPrescriptionTemplatesQuery { TenantId = tenantId, SearchTerm = requestedName.Trim() }, cancellationToken);
        PrescriptionTemplateSummaryDto[] exact = templates.Where(x => string.Equals(x.Name.Trim(), requestedName.Trim(), StringComparison.OrdinalIgnoreCase)).ToArray();
        IReadOnlyList<PrescriptionTemplateSummaryDto> selected = exact.Length > 0 ? exact : templates;
        return new(selected.Count == 0 ? PrescriptionTemplateSearchStatus.NotFound : selected.Count == 1 && exact.Length == 1 ? PrescriptionTemplateSearchStatus.ExactMatch : PrescriptionTemplateSearchStatus.Ambiguous,
            selected.Select(x => new PrescriptionTemplateMatch(x.Id, x.Name, x.Description, Version(x))).ToArray());
    }

    internal static string Version(PrescriptionTemplateSummaryDto template) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{template.Id:D}|{template.UpdatedAt ?? template.CreatedAt:O}|{template.ItemCount}")))[..16];
}

public sealed class GetPrescriptionTemplateRequirements(ISender sender)
{
    public async Task<PrescriptionTemplateRequirements?> ExecuteAsync(Guid templateId, string tenantId, CancellationToken cancellationToken)
    {
        PrescriptionTemplateDto? template = await sender.Send(new GetPrescriptionTemplateQuery { TemplateId = templateId, TenantId = tenantId }, cancellationToken);
        if (template is null || template.Status != PrescriptionTemplateStatus.Active) return null;
        return new(template.Id, template.Name, SearchPrescriptionTemplatesForAgent.Version(template), template.Items, DetectMissing(template.Items));
    }

    internal static IReadOnlyList<string> DetectMissing(IEnumerable<PrescriptionTemplateItemDto> items)
    {
        HashSet<string> missing = new(StringComparer.Ordinal);
        foreach (PrescriptionTemplateItemDto item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ReferenceCode) && string.IsNullOrWhiteSpace(item.DisplayName)) _ = missing.Add("medication");
            if (item.Category == PrescriptionItemCategory.Drug)
            {
                if (string.IsNullOrWhiteSpace(item.DosageJson))
                {
                    _ = missing.Add("dosage"); _ = missing.Add("frequency"); _ = missing.Add("route"); _ = missing.Add("instructions");
                }
                if (item.DurationDays is null or <= 0) _ = missing.Add("duration");
            }
        }
        return missing.ToArray();
    }
}

public sealed class PreparePrescriptionDraftFromTemplate(ISender sender, GetPrescriptionTemplateRequirements requirements, IPrescriptionAgentReviewStore store)
{
    public async Task<PrescriptionDraftPreparationResult> ExecuteAsync(Guid templateId, string? insuranceCoverage, PrescriptionAgentScope scope, PrescriptionAgentProvenance provenance, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(insuranceCoverage)) return new(PrescriptionDraftPreparationStatus.MissingInsuranceCoverage, Message: "The clinician must explicitly name insurance coverage.");
        PrescriptionTemplateRequirements? template = await requirements.ExecuteAsync(templateId, scope.TenantId, cancellationToken);
        if (template is null || !string.Equals(template.TemplateVersion, provenance.TemplateVersion, StringComparison.Ordinal)) return new(PrescriptionDraftPreparationStatus.InvalidTemplate, Message: "The template is inactive, missing, or changed.");
        if (template.MissingFields.Count > 0) return new(PrescriptionDraftPreparationStatus.MissingInformation, MissingFields: template.MissingFields, Message: "Material prescription information is missing; ask the clinician rather than infer it.");
        CreateDraftResult created = await sender.Send(new CreateDraftCommand { PatientId = scope.PatientId, DoctorId = scope.DoctorId, ClinicId = scope.ClinicId, TenantId = scope.TenantId, SelectedProviderKey = insuranceCoverage.Trim() }, cancellationToken);
        if (!created.IsSuccess || created.PrescriptionId is not Guid prescriptionId) return new(PrescriptionDraftPreparationStatus.Failed, Message: created.ErrorMessage);
        PrescriptionTemplateCommandResult loaded = await sender.Send(new LoadPrescriptionTemplateIntoDraftCommand { TemplateId = templateId, PrescriptionId = prescriptionId, TenantId = scope.TenantId }, cancellationToken);
        if (!loaded.IsSuccess) return new(PrescriptionDraftPreparationStatus.Failed, Message: loaded.ErrorMessage);
        PrescriptionDto? draft = await sender.Send(new GetPrescriptionQuery { PrescriptionId = prescriptionId }, cancellationToken);
        if (draft is null || draft.PatientId != scope.PatientId) return new(PrescriptionDraftPreparationStatus.Failed, Message: "The prepared Medical draft could not be reviewed.");
        PrescriptionDraftReview review = new(Guid.NewGuid(), prescriptionId, templateId, template.TemplateVersion, insuranceCoverage.Trim(), draft.Items, provenance);
        await store.SaveAsync(review, cancellationToken);
        return new(PrescriptionDraftPreparationStatus.Prepared, review);
    }
}

public sealed class ConfirmPrescriptionDraft(ISender sender, GetPrescriptionTemplateRequirements requirements, IPrescriptionAgentReviewStore store)
{
    public async Task<PrescriptionConfirmationResult> ExecuteAsync(Guid reviewId, PrescriptionAgentScope scope, string confirmedByUserId, CancellationToken cancellationToken)
    {
        PrescriptionConfirmationResult? prior = await store.GetConfirmationAsync(reviewId, cancellationToken);
        if (prior is not null) return prior with { Status = PrescriptionConfirmationStatus.AlreadyConfirmed };
        PrescriptionDraftReview? review = await store.GetAsync(reviewId, cancellationToken);
        if (review is null) return new(PrescriptionConfirmationStatus.NotFound);
        if (review.Provenance.RequestedByUserId != scope.RequestedByUserId) return new(PrescriptionConfirmationStatus.Forbidden, Message: "Only the requesting clinician may confirm this review.");
        PrescriptionDto? draft = await sender.Send(new GetPrescriptionQuery { PrescriptionId = review.PrescriptionId }, cancellationToken);
        if (draft is null || draft.PatientId != scope.PatientId || draft.DoctorId != scope.DoctorId || draft.ClinicId != scope.ClinicId) return new(PrescriptionConfirmationStatus.Forbidden);
        PrescriptionTemplateRequirements? current = await requirements.ExecuteAsync(review.TemplateId, scope.TenantId, cancellationToken);
        if (current is null || current.TemplateVersion != review.TemplateVersion) return new(PrescriptionConfirmationStatus.ExpiredOrChangedTemplate, review.PrescriptionId);
        review = review with { Provenance = review.Provenance with { ConfirmedByUserId = confirmedByUserId } };
        await store.SaveAsync(review, cancellationToken);
        ValidateDraftResult validation = await sender.Send(new ValidateDraftCommand { PrescriptionId = review.PrescriptionId }, cancellationToken);
        if (!validation.IsValid) return new(PrescriptionConfirmationStatus.InvalidDraft, review.PrescriptionId, Message: string.Join(" ", validation.Errors));
        SubmitPrescriptionResult submitted = await sender.Send(new SubmitPrescriptionCommand { PrescriptionId = review.PrescriptionId, CorrelationId = $"agent:{review.Provenance.AgentRunId}:{review.Provenance.ToolInvocationId}" }, cancellationToken);
        PrescriptionConfirmationResult result = submitted.IsSuccess
            ? new(PrescriptionConfirmationStatus.Submitted, review.PrescriptionId, submitted.ResultStatus, submitted.TrackingCode)
            : new(PrescriptionConfirmationStatus.SubmissionFailed, review.PrescriptionId, submitted.ResultStatus, Message: submitted.ErrorMessage);
        await store.SaveConfirmationAsync(reviewId, result, cancellationToken);
        return result;
    }
}
