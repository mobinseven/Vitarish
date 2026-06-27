namespace Vitarish.Clinic.Interface.Models;

internal static class PatientProjectMappings
{
    public static PatientReadDto ToPatientRead(this ProjectReadDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        AvatarFileId = source.AvatarFileId,
        IdentificationCode = source.IdentificationCode,
        Template = source.Template,
        UserIds = source.UserIds,
        CreatedAt = source.CreatedAt,
        CreatedBy = source.CreatedBy,
        UpdatedAt = source.UpdatedAt,
        UpdatedBy = source.UpdatedBy,
        DeletedAt = source.DeletedAt,
        DeletedBy = source.DeletedBy,
        Metadata = source.Metadata,
        MetadataValues = source.MetadataValues,
        Stats = source.Stats
    };

    public static PatientWriteDto ToPatientWrite(this ProjectReadDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        AvatarFileId = source.AvatarFileId,
        IdentificationCode = source.IdentificationCode,
        Template = source.Template,
        UserIds = source.UserIds,
        Metadata = source.Metadata,
        MetadataValues = source.MetadataValues
    };

    public static ProjectReadDto ToProjectRead(this PatientViewDto source) => new()
    {
        Id = source.Id,
        Name = source.Name,
        Description = source.Description,
        AvatarFileId = source.AvatarFileId,
        IdentificationCode = source.IdentificationCode,
        Template = source.Template,
        UserIds = source.UserIds,
        CreatedAt = source.CreatedAt,
        CreatedBy = source.CreatedBy,
        UpdatedAt = source.UpdatedAt,
        UpdatedBy = source.UpdatedBy,
        DeletedAt = source.DeletedAt,
        DeletedBy = source.DeletedBy,
        Stats = source.Stats,
        Metadata = ToPatientMetadata(source),
        //MetadataValues = ToPatientMetadata(source)
    };

    private static Dictionary<string, object?> ToPatientMetadata(PatientViewDto source) => new()
    {
        [nameof(PatientReadDto.FirstName)] = source.FirstName,
        [nameof(PatientReadDto.LastName)] = source.LastName,
        [nameof(PatientReadDto.DateOfBirth)] = source.DateOfBirth,
        [nameof(PatientReadDto.DateOfSurgery)] = source.DateOfSurgery,
        [nameof(PatientReadDto.Gender)] = source.Gender,
        [nameof(PatientReadDto.PlaceOfBirth)] = source.PlaceOfBirth,
        [nameof(PatientReadDto.Address)] = source.Address,
        [nameof(PatientReadDto.Education)] = source.Education,
        [nameof(PatientReadDto.Occupation)] = source.Occupation,
        [nameof(PatientReadDto.FatherName)] = source.FatherName,
        [nameof(PatientReadDto.Height)] = source.Height,
        [nameof(PatientReadDto.Weight)] = source.Weight,
        [nameof(PatientReadDto.Email)] = source.Email,
        [nameof(PatientReadDto.Phone)] = source.Phone,
        [nameof(PatientReadDto.Referrer)] = source.Referrer,
        [nameof(PatientReadDto.InsuranceProvider)] = source.InsuranceProvider,
        [nameof(PatientReadDto.VisitReason)] = source.VisitReason,
        [nameof(PatientReadDto.Allergies)] = source.Allergies,
        [nameof(PatientReadDto.CurrentMedications)] = source.CurrentMedications,
        [nameof(PatientReadDto.ClinicalNotes)] = source.ClinicalNotes,
        [nameof(PatientReadDto.EmergencyContactName)] = source.EmergencyContactName,
        [nameof(PatientReadDto.EmergencyContactPhone)] = source.EmergencyContactPhone
    };
}
