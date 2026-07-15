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
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        DateOfSurgery = patient.DateOfSurgery,
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
