using System.Text.Json.Serialization;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Clinic.Infrastructure.ClinicWorkspace.Capabilities.Prescriptions;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PatientPrescriptionHistoryResponse))]
[JsonSerializable(typeof(PatientPrescriptionDetailResponse))]
[JsonSerializable(typeof(PatientPrescriptionAttentionResponse))]
internal sealed partial class PatientPrescriptionReadJsonSerializerContext : JsonSerializerContext;

internal sealed record PatientPrescriptionSummary(Guid Id, string Status, string? SelectedProviderKey, int ItemCount, DateTime CreatedAt, DateTime? SubmittedAt, string? TrackingCode, IReadOnlyList<string> ItemDisplayNames, DateTime SourceUpdatedAt);
internal sealed record PatientPrescriptionHistoryResponse(IReadOnlyList<PatientPrescriptionSummary> Prescriptions, bool HasMore);
internal sealed record PatientPrescriptionDetailResponse(bool Found, PatientPrescriptionDetail? Prescription = null);
internal sealed record PatientPrescriptionDetail(Guid Id, string Status, string? SelectedProviderKey, int VersionNumber, DateTime CreatedAt, DateTime? SubmittedAt, string? TrackingCode, IReadOnlyList<PatientPrescriptionItem> Items, bool HasMoreItems, DateTime SourceUpdatedAt);
internal sealed record PatientPrescriptionItem(PrescriptionItemCategory Category, string? ReferenceCode, string? DisplayName, int Quantity, int? DurationDays, int SortOrder);
internal sealed record PatientPrescriptionAttentionResponse(int TotalCount, int AttentionCount, IReadOnlyList<PatientPrescriptionSummary> Attention, bool HasMore);
