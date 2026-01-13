namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;

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
