using System.Text.Json.Serialization;
using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Infrastructure.Serializers;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata,
    PropertyNameCaseInsensitive = true,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(SurgeryCatalogData))]
[JsonSerializable(typeof(SurgeryReminderPolicy))]
internal sealed partial class SurgeryCatalogJsonSerializerContext : JsonSerializerContext;

/// <summary>The stored shape of a clinic-managed surgery type; IsDefault marks the seeded default type.</summary>
internal sealed record SurgeryCatalogData(
    string Name,
    IReadOnlyList<string> Tools,
    IReadOnlyList<string> DefaultTeamRoles,
    IReadOnlyList<SurgeryReminderPolicy> ReminderPolicies,
    bool IsDefault);
