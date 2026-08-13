using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Communications;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PatientReminderStatusResponse))]
internal sealed partial class PatientReminderReadJsonSerializerContext : JsonSerializerContext;

internal sealed record PatientReminderStatusResponse(bool Found, IReadOnlyList<PatientReminderStatus>? Reminders = null, bool HasMore = false);
internal sealed record PatientReminderStatus(Guid Id, string Kind, string SourceKind, string TemplatePurpose, DateTime RemindAt, DateTime? SentAt, bool IsSent, DateTime? ProviderSubmittedAt, DateTime SourceUpdatedAt);
