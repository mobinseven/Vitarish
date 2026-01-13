using Vitarish.Sea.Interface.Plugins.Base.Enums;

namespace Vitarish.Sea.Interface.Plugins.Gym.Projects;

public sealed record GymProjectMetadata
{
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? DateOfBirth { get; init; }

    public Gender? Gender { get; init; }

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
