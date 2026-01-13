namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;

internal static class GymProjectMetadataParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool HasGymMetadata(string? metadataJson) => TryParse(metadataJson) is not null || TryParseTemplate(metadataJson) is not null;

    public static GymProjectMetadata? TryParse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            GymProjectMetadata? metadata = JsonSerializer.Deserialize<GymProjectMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.FirstName)
                && string.IsNullOrWhiteSpace(metadata.LastName)
                && string.IsNullOrWhiteSpace(metadata.MembershipType)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static GymTemplateMetadata? TryParseTemplate(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            GymTemplateMetadata? metadata = JsonSerializer.Deserialize<GymTemplateMetadata>(metadataJson, SerializerOptions);
            return metadata is null
                ? null
                : string.IsNullOrWhiteSpace(metadata.DefaultMembershipType)
                && string.IsNullOrWhiteSpace(metadata.DefaultPreferredProgram)
                && string.IsNullOrWhiteSpace(metadata.OnboardingChecklist)
                ? null
                : metadata;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
