using Vitarish.Sea.Interface.Plugins.Clinic.Models;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Extensions;

internal static class ClinicProjectMetadataParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

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
}
