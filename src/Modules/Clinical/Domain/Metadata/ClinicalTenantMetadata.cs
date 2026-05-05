using System.Text.Json;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Domain.Options;

namespace Vitarish.Clinical.Domain.Metadata;

public static class ClinicalTenantMetadata
{
    public const string EnabledKey = "clinical.enabled";
    public const string EndpointOverrideKey = "clinical.fhir.baseUrl";
    public const string EnvironmentKey = "clinical.environment";
    public const string OrganizationKey = "clinical.organization";
    public const string ClinicKey = "clinical.clinic";
    public const string RequireAccessReasonKey = "clinical.requireAccessReason";
    public const string AllowedResourceTypesKey = "clinical.allowedResourceTypes";

    public static ClinicalTenantSettings Read(
        IReadOnlyDictionary<string, object?>? metadata,
        ClinicalOptions? defaults = null)
    {
        metadata ??= new Dictionary<string, object?>();

        bool enabled = TryGetBoolean(metadata, EnabledKey) ?? defaults?.Enabled ?? false;
        bool requireAccessReason = TryGetBoolean(metadata, RequireAccessReasonKey) ?? defaults?.DefaultRequireAccessReason ?? true;
        string? endpointOverride = TryGetString(metadata, EndpointOverrideKey);
        string? environment = TryGetString(metadata, EnvironmentKey);
        string? organization = TryGetString(metadata, OrganizationKey);
        string? clinic = TryGetString(metadata, ClinicKey);
        IReadOnlyList<string> allowedResourceTypes = TryGetStringList(metadata, AllowedResourceTypesKey)
            ?? defaults?.AllowedResourceTypes?.ToArray()
            ?? [];

        return new ClinicalTenantSettings
        {
            Enabled = enabled,
            EndpointOverride = endpointOverride,
            Environment = environment,
            Organization = organization,
            Clinic = clinic,
            RequireAccessReason = requireAccessReason,
            AllowedResourceTypes = allowedResourceTypes
        };
    }

    private static bool? TryGetBoolean(IReadOnlyDictionary<string, object?> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            bool boolean => boolean,
            string text when bool.TryParse(text, out bool parsed) => parsed,
            JsonElement json when json.ValueKind is JsonValueKind.True or JsonValueKind.False => json.GetBoolean(),
            _ => null
        };
    }

    private static string? TryGetString(IReadOnlyDictionary<string, object?> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text when !string.IsNullOrWhiteSpace(text) => text.Trim(),
            JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
            _ => value.ToString()
        };
    }

    private static IReadOnlyList<string>? TryGetStringList(IReadOnlyDictionary<string, object?> metadata, string key)
    {
        if (!metadata.TryGetValue(key, out object? value) || value is null)
        {
            return null;
        }

        return value switch
        {
            string text => text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            JsonElement json when json.ValueKind == JsonValueKind.Array => json
                .EnumerateArray()
                .Where(item => item.ValueKind == JsonValueKind.String)
                .Select(item => item.GetString())
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            IEnumerable<object?> values => values
                .Select(item => item?.ToString())
                .OfType<string>()
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            _ => null
        };
    }
}
