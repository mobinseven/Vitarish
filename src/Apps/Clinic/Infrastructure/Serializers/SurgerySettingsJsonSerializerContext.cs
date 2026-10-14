using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Infrastructure.Serializers;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SurgerySettingsData))]
internal sealed partial class SurgerySettingsJsonSerializerContext : JsonSerializerContext;

/// <summary>The stored shape of a clinic's surgery settings.</summary>
internal sealed record SurgerySettingsData(
    string CancellationSubject,
    string CancellationTextBody);
