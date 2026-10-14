using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Patients;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ClinicPatientOverviewResponse))]
[JsonSerializable(typeof(ClinicPatientProfileResponse))]
[JsonSerializable(typeof(ClinicPatientFactsResponse))]
internal sealed partial class ClinicPatientReadJsonSerializerContext : JsonSerializerContext;

internal sealed record ClinicPatientOverviewResponse(bool Found, DateTimeOffset? SourceUpdatedAt = null, ClinicPatientOverview? Overview = null);
internal sealed record ClinicPatientOverview(string Name, DateTime? DateOfBirth, string? VisitReason, bool HasRecordedAllergies, bool HasRecordedMedications);
internal sealed record ClinicPatientProfileResponse(bool Found, DateTimeOffset? SourceUpdatedAt = null, ClinicPatientProfile? Profile = null);
internal sealed record ClinicPatientProfile(string Name, string? FirstName, string? LastName, DateTime? DateOfBirth, string Gender, string? Phone, string? Email, string? Address, string? InsuranceProvider);
internal sealed record ClinicPatientFactsResponse(bool Found, DateTimeOffset? SourceUpdatedAt = null, ClinicPatientFacts? Facts = null);
internal sealed record ClinicPatientFacts(string? VisitReason, string? Allergies, string? CurrentMedications, string? ClinicalNotes);
