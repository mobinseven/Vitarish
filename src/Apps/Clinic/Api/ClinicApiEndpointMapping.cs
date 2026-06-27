using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Contracts.Errors;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Modules.Aggregator.Core.Endpoints;
using ContractEducation = Vitarish.Clinic.Contracts.Enums.Education;
using ContractGender = Vitarish.Clinic.Contracts.Enums.Gender;
using ContractPatientSortBy = Vitarish.Clinic.Contracts.Enums.PatientSortBy;
using ContractProgressState = Vitarish.Sea.Contracts.Enums.ProgressState;
using ContractStatsReadDto = Vitarish.Sea.Contracts.Responses.StatsReadDto;
using DomainEducation = Vitarish.Clinic.Domain.Enums.Education;
using DomainGender = Vitarish.Clinic.Domain.Enums.Gender;
using DomainPatientSortBy = Vitarish.Clinic.Domain.Enums.PatientSortBy;

namespace Vitarish.Clinic.Api;

public static class ClinicApiEndpointMapping
{
    private const int DefaultSkip = 0;
    private const int DefaultTake = 100;

    public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ClinicApiRoutes.Base);

        _ = group.MapModuleGet(ClinicApiRoutes.Health, () => Results.Ok(), "ClinicGetHealth");
        _ = group.MapModulePost(ClinicApiRoutes.Patients, ListPatientsAsync, "ClinicListPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientCount, CountPatientsAsync, "ClinicCountPatients", typeof(int));
        _ = group.MapModulePost(ClinicApiRoutes.PatientSearch, SearchPatientsAsync, "ClinicSearchPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientIdentificationLookup, LookupPatientByIdentificationCodeAsync, "ClinicLookupPatientByIdentificationCode", typeof(PatientViewDto));
        _ = group.MapModulePost(ClinicApiRoutes.PatientFindOrCreate, FindOrCreatePatientAsync, "ClinicFindOrCreatePatient", typeof(PatientViewDto));
        _ = group.MapModuleGet(ClinicApiRoutes.PatientDetails, GetPatientDetailsAsync, "ClinicGetPatientDetails", typeof(PatientViewDto));

        return endpoints;
    }

    private static async Task<IResult> ListPatientsAsync(PatientListRequest request, ISender sender, CancellationToken ct)
    {
        if (request.Skip is < 0 || request.Take is < 0)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Skip and take must be non-negative."));
        }

        IReadOnlyList<PatientView> patients = await sender.Send(
            new GetPatientsQuery(
                request.Search,
                request.Skip ?? DefaultSkip,
                request.Take ?? DefaultTake,
                request.Template ?? false,
                ToDomain(request.SortBy),
                request.Descending),
            ct);

        return Results.Ok(patients.Select(ToContract).ToList());
    }

    private static async Task<IResult> CountPatientsAsync(PatientCountRequest request, ISender sender, CancellationToken ct)
    {
        int count = await sender.Send(new CountPatientsQuery(request.Search, request.Template), ct);
        return Results.Ok(count);
    }

    private static async Task<IResult> SearchPatientsAsync(PatientSearchRequest request, ISender sender, CancellationToken ct)
    {
        if (request.Skip is < 0 || request.Take is < 0)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Skip and take must be non-negative."));
        }

        IReadOnlyList<PatientView> patients = await sender.Send(
            new GetPatientsQuery(
                request.Search,
                request.Skip ?? DefaultSkip,
                request.Take ?? DefaultTake,
                request.Template ?? false,
                DomainPatientSortBy.Name,
                false),
            ct);

        return Results.Ok(patients.Select(ToContract).ToList());
    }

    private static async Task<IResult> LookupPatientByIdentificationCodeAsync(PatientIdentificationLookupRequest request, ISender sender, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdentificationCode))
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Identification code is required."));
        }

        PatientView? patient = await sender.Send(new GetPatientByIdentificationCodeQuery(request.IdentificationCode), ct);
        return patient is null
            ? Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient was not found."))
            : Results.Ok(ToContract(patient));
    }

    private static async Task<IResult> FindOrCreatePatientAsync(PatientFindOrCreateRequest request, ISender sender, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.IdentificationCode))
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Identification code is required to find a patient."));
        }

        PatientView? patient = await sender.Send(new GetPatientByIdentificationCodeQuery(request.IdentificationCode), ct);
        return patient is null
            ? Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient creation is not available through the Clinic API yet."))
            : Results.Ok(ToContract(patient));
    }

    private static async Task<IResult> GetPatientDetailsAsync(Guid patientId, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Patient id is required."));
        }

        PatientView? patient = await sender.Send(new GetPatientByIdQuery(patientId), ct);
        return patient is null
            ? Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient was not found."))
            : Results.Ok(ToContract(patient));
    }

    private static DomainPatientSortBy? ToDomain(ContractPatientSortBy? sortBy) => sortBy switch
    {
        ContractPatientSortBy.Name => DomainPatientSortBy.Name,
        ContractPatientSortBy.Template => DomainPatientSortBy.Template,
        ContractPatientSortBy.IdentificationCode => DomainPatientSortBy.IdentificationCode,
        ContractPatientSortBy.DateOfBirth => DomainPatientSortBy.DateOfBirth,
        ContractPatientSortBy.DateOfSurgery => DomainPatientSortBy.DateOfSurgery,
        ContractPatientSortBy.CreatedAt => DomainPatientSortBy.CreatedAt,
        ContractPatientSortBy.UpdatedAt => DomainPatientSortBy.UpdatedAt,
        _ => null
    };

    private static PatientViewDto ToContract(PatientView patient) => new()
    {
        Id = patient.Id,
        TenantId = patient.TenantId,
        Name = patient.Name,
        Description = patient.Description,
        AvatarFileId = patient.AvatarFileId,
        IdentificationCode = patient.IdentificationCode,
        Template = patient.Template,
        CreatedAt = patient.CreatedAt,
        CreatedBy = patient.CreatedBy,
        UpdatedAt = patient.UpdatedAt,
        UpdatedBy = patient.UpdatedBy,
        DeletedAt = patient.DeletedAt,
        DeletedBy = patient.DeletedBy,
        IsDeleted = patient.IsDeleted,
        UserIds = patient.UserIds,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        DateOfSurgery = patient.DateOfSurgery,
        Gender = (ContractGender)(int)patient.Gender,
        PlaceOfBirth = patient.PlaceOfBirth,
        Address = patient.Address,
        Education = (ContractEducation)(int)patient.Education,
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
        Stats = ToContract(patient.Stats)
    };

    private static ContractStatsReadDto? ToContract(Vitarish.Sea.Domain.Dto.StatsReadDto? stats) => stats is null
        ? null
        : new()
        {
            State = stats.State is null ? null : (ContractProgressState)(int)stats.State.Value,
            Procedures = stats.Procedures,
            Events = stats.Events,
            CommittedEvents = stats.CommittedEvents,
            EventItems = stats.EventItems,
            NewItems = stats.NewItems,
            Unread = stats.Unread,
            LastUpdate = stats.LastUpdate
        };
}
