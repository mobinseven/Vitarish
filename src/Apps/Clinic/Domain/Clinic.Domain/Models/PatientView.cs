using System.ComponentModel.DataAnnotations.Schema;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Domain.Models;

public sealed class PatientView
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? AvatarFileId { get; init; }
    public string? IdentificationCode { get; init; }
    public bool Template { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public DateTime? DeletedAt { get; init; }
    public string? DeletedBy { get; init; }
    public bool IsDeleted => DeletedAt is not null;
    public string[] UserIds { get; init; } = [];
    public string[] Tags { get; init; } = [];
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public Gender Gender { get; init; }
    public string? PlaceOfBirth { get; init; }
    public string? Address { get; init; }
    public Education Education { get; init; }
    public string? Occupation { get; init; }
    public string? FatherName { get; init; }
    public int Height { get; init; }
    public int Weight { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Referrer { get; init; }
    public string? InsuranceProvider { get; init; }
    public string? VisitReason { get; init; }
    public string? Allergies { get; init; }
    public string? CurrentMedications { get; init; }
    public string? ClinicalNotes { get; init; }
    public string? EmergencyContactName { get; init; }
    public string? EmergencyContactPhone { get; init; }

    [NotMapped]
    public StatsReadDto? Stats { get; set; }

    /// <summary>Start of the latest non-cancelled surgery that already started; loaded per page.</summary>
    [NotMapped]
    public DateTimeOffset? LastSurgeryAt { get; set; }

    /// <summary>Start of the earliest planned surgery still ahead; loaded per page.</summary>
    [NotMapped]
    public DateTimeOffset? NextSurgeryAt { get; set; }
}
