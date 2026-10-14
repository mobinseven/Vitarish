using Microsoft.Extensions.Logging;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Abstractions;
using SeaProjectDescriptorDto = Vitarish.Sea.Domain.Dto.ProjectDescriptorDto;
using SeaProjectReadDto = Vitarish.Sea.Domain.Dto.ProjectReadDto;

namespace Vitarish.Clinic.Infrastructure.Handlers;

internal sealed class SeaPatientDirectory(
    IProjectsService projects,
    CreatePatientVisitsProcedureHandler createVisitsProcedure,
    IRequestContext requestContext,
    ILogger<SeaPatientDirectory> logger) : IPatientDirectory
{
    public async Task<PatientView?> FindByIdentificationCodeAsync(
        string identificationCode,
        CancellationToken cancellationToken)
    {
        EntityResult<IReadOnlyList<SeaProjectReadDto>> result = await projects.GetListAsync(
            new SeaProjectDescriptorDto
            {
                IdentificationCode = identificationCode,
                Template = false,
                Skip = 0,
                Take = 1
            },
            cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException("The patient directory could not be queried.");
        }

        SeaProjectReadDto? project = result.Value?.FirstOrDefault();
        return project is null ? null : ToPatientView(project, project.ToPatientWrite());
    }

    public async Task<PatientCreationResult> CreateAsync(
        PatientWriteDto patient,
        CancellationToken cancellationToken)
    {
        EntityResult<SeaProjectReadDto> result = await projects.AddAsync(patient, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            if (!await createVisitsProcedure.CreateAsync(result.Value.Id, cancellationToken))
            {
                return new PatientCreationResult.Failure(
                    PatientCreationFailureReason.Unexpected,
                    "The patient visits procedure could not be created.");
            }

            return new PatientCreationResult.Success(ToPatientView(result.Value, patient));
        }

        EntityError? error = result.Errors.FirstOrDefault();
        PatientCreationFailureReason reason = error?.Code switch
        {
            EntityErrorCode.Validation => PatientCreationFailureReason.Validation,
            EntityErrorCode.Conflict or EntityErrorCode.Concurrency => PatientCreationFailureReason.Conflict,
            _ => PatientCreationFailureReason.Unexpected
        };

        if (reason == PatientCreationFailureReason.Unexpected)
        {
            logger.LogError("Sea failed to create a patient project with error code {ErrorCode}.", error?.Code);
        }

        return new PatientCreationResult.Failure(
            reason,
            error?.Message ?? "The patient project could not be created.");
    }

    public async Task<PatientUpdateResult> UpdateAsync(
        PatientWriteDto patient,
        CancellationToken cancellationToken)
    {
        EntityResult<SeaProjectReadDto> existingResult = await projects.GetAsync(patient.Id, cancellationToken);
        if (existingResult.IsFailure)
        {
            if (existingResult.Errors.FirstOrDefault()?.Code == EntityErrorCode.NotFound)
            {
                return new PatientUpdateResult.NotFound();
            }

            return new PatientUpdateResult.Failure(
                PatientUpdateFailureReason.Unexpected,
                existingResult.Errors.FirstOrDefault()?.Message ?? "Failed to retrieve the existing patient.");
        }

        SeaProjectReadDto? existing = existingResult.Value;
        if (existing is null)
        {
            return new PatientUpdateResult.NotFound();
        }

        PatientWriteDto updated = existing.ToPatientWrite();
        updated.Metadata.Remove(PatientLegacyMetadataKeys.DateOfSurgery);
        updated.IdentificationCode = patient.IdentificationCode;
        updated.Name = patient.Name;
        updated.Template = patient.Template;
        updated.FirstName = patient.FirstName;
        updated.LastName = patient.LastName;
        updated.Phone = patient.Phone;
        updated.Email = patient.Email;
        updated.DateOfBirth = patient.DateOfBirth;
        updated.Gender = patient.Gender;
        updated.PlaceOfBirth = patient.PlaceOfBirth;
        updated.Address = patient.Address;
        updated.Education = patient.Education;
        updated.Occupation = patient.Occupation;
        updated.FatherName = patient.FatherName;
        updated.Height = patient.Height;
        updated.Weight = patient.Weight;
        updated.Referrer = patient.Referrer;
        updated.InsuranceProvider = patient.InsuranceProvider;
        updated.VisitReason = patient.VisitReason;
        updated.Allergies = patient.Allergies;
        updated.CurrentMedications = patient.CurrentMedications;
        updated.ClinicalNotes = patient.ClinicalNotes;
        updated.EmergencyContactName = patient.EmergencyContactName;
        updated.EmergencyContactPhone = patient.EmergencyContactPhone;
        updated.Tags = patient.Tags;

        EntityResult<SeaProjectReadDto> result = await projects.UpdateAsync(updated, cancellationToken);
        if (result.IsSuccess && result.Value is not null)
        {
            return new PatientUpdateResult.Success(ToPatientView(result.Value, updated));
        }

        EntityError? error = result.Errors.FirstOrDefault();
        PatientUpdateFailureReason reason = error?.Code switch
        {
            EntityErrorCode.Validation => PatientUpdateFailureReason.Validation,
            EntityErrorCode.Conflict or EntityErrorCode.Concurrency => PatientUpdateFailureReason.Conflict,
            _ => PatientUpdateFailureReason.Unexpected
        };

        if (reason == PatientUpdateFailureReason.Unexpected)
        {
            logger.LogError("Sea failed to update a patient project with error code {ErrorCode}.", error?.Code);
        }

        return new PatientUpdateResult.Failure(
            reason,
            error?.Message ?? "The patient project could not be updated.");
    }

    public async Task<PatientView?> FindByIdAsync(Guid patientId, CancellationToken cancellationToken)
    {
        EntityResult<SeaProjectReadDto> result = await projects.GetAsync(patientId, cancellationToken);
        return result.IsSuccess && result.Value is not null && !result.Value.Template
            ? ToPatientView(result.Value, result.Value.ToPatientWrite())
            : null;
    }

    private PatientView ToPatientView(SeaProjectReadDto project, PatientWriteDto patient) => new()
    {
        Id = project.Id,
        TenantId = requestContext.TenantId
            ?? throw new InvalidOperationException("Tenant ID is required to map a patient record."),
        Name = project.Name,
        Description = project.Description,
        AvatarFileId = patient.AvatarFileId,
        IdentificationCode = project.IdentificationCode,
        Template = project.Template,
        CreatedAt = project.CreatedAt,
        CreatedBy = project.CreatedBy,
        UpdatedAt = project.UpdatedAt,
        UpdatedBy = project.UpdatedBy,
        DeletedAt = project.DeletedAt,
        DeletedBy = project.DeletedBy,
        UserIds = [.. project.UserIds],
        Tags = PatientTags.Normalize(patient.Tags),
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        Gender = patient.Gender,
        PlaceOfBirth = patient.PlaceOfBirth,
        Address = patient.Address,
        Education = patient.Education,
        Occupation = patient.Occupation,
        FatherName = patient.FatherName,
        Height = patient.Height,
        Weight = patient.Weight,
        Email = patient.Email,
        Phone = patient.Phone,
        Referrer = patient.Referrer,
        InsuranceProvider = patient.InsuranceProvider,
        VisitReason = patient.VisitReason,
        Allergies = patient.Allergies,
        CurrentMedications = patient.CurrentMedications,
        ClinicalNotes = patient.ClinicalNotes,
        EmergencyContactName = patient.EmergencyContactName,
        EmergencyContactPhone = patient.EmergencyContactPhone,
        Stats = project.Stats
    };
}
