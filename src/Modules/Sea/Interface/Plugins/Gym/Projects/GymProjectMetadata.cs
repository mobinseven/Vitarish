namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;

internal static class GymProjectMetadataParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool HasGymMetadata(string? metadataJson) => TryParse(metadataJson) is not null || TryParseTemplate(metadataJson) is not null;

    public static GymTraineeMetadata? TryParse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            GymTraineeMetadata? metadata = JsonSerializer.Deserialize<GymTraineeMetadata>(metadataJson, SerializerOptions);
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

internal sealed record GymTraineeMetadata
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DateOfBirth { get; init; }

    public string? Gender { get; init; }

    public string? Nationality { get; init; }

    public string? Email { get; init; }

    public string? Phone { get; init; }

    public string? MembershipType { get; init; }

    public string? PreferredProgram { get; init; }

    public string? Goals { get; init; }

    public string? EmergencyContactName { get; init; }

    public string? EmergencyContactPhone { get; init; }

    public string? MedicalNotes { get; init; }

    public Guid? FacilityId { get; init; }

    public Guid? TemplateId { get; init; }
}

public sealed record GymTemplateMetadata
{
    public string? DefaultMembershipType { get; init; }

    public string? DefaultPreferredProgram { get; init; }

    public string? DefaultGoals { get; init; }

    public string? OnboardingChecklist { get; init; }

    public string? SafetyGuidelines { get; init; }

    public bool IncludeEmergencyContact { get; init; }

    public bool IncludeMedicalNotes { get; init; }

    public Guid? FacilityId { get; init; }

    public Guid? TemplateId { get; init; }
}
