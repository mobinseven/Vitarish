namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

internal static class ClinicProjectMetadataParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool HasClinicMetadata(string? metadataJson) => TryParsePatient(metadataJson) is not null || TryParseTemplate(metadataJson) is not null;

    public static ClinicProjectMetadata? TryParsePatient(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            ClinicProjectMetadata? metadata = JsonSerializer.Deserialize<ClinicProjectMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.FirstName)
                && string.IsNullOrWhiteSpace(metadata.LastName)
                && string.IsNullOrWhiteSpace(metadata.VisitReason)
                && string.IsNullOrWhiteSpace(metadata.InsuranceProvider)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static ClinicTemplateProjectMetadata? TryParseTemplate(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            ClinicTemplateProjectMetadata? metadata = JsonSerializer.Deserialize<ClinicTemplateProjectMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.DefaultVisitReason)
                && string.IsNullOrWhiteSpace(metadata.IntakeChecklist)
                && string.IsNullOrWhiteSpace(metadata.BaselineConsentText)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
