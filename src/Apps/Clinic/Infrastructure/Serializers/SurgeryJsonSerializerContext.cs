using System.Text.Json.Serialization;
using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Infrastructure.Serializers;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SurgeryWriteModel))]
[JsonSerializable(typeof(SurgeryReadModel))]
[JsonSerializable(typeof(SurgeryContact))]
[JsonSerializable(typeof(SurgeryTeamMember))]
[JsonSerializable(typeof(IReadOnlyList<SurgeryTeamMember>))]
[JsonSerializable(typeof(SurgeryReminderPolicy))]
[JsonSerializable(typeof(SurgeryReminderTemplateVariables))]
internal sealed partial class SurgeryJsonSerializerContext : JsonSerializerContext;

internal sealed record SurgeryReminderTemplateVariables(Guid SurgeryId, string ContactName);
