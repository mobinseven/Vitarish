using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Commands.Visits;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Contracts.Errors;
using Vitarish.Clinic.Contracts.PatientAgent;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Modules.Base.Core.Endpoints;
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
    private const string SeaManagePolicy = "Sea.Manage";

    public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ClinicApiRoutes.Base);

        _ = group.MapModuleGet(ClinicApiRoutes.Health, () => Results.Ok(), "ClinicGetHealth");
        _ = group.MapModulePost(ClinicApiRoutes.Patients, ListPatientsAsync, "ClinicListPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientCount, CountPatientsAsync, "ClinicCountPatients", typeof(int));
        _ = group.MapModulePost(ClinicApiRoutes.PatientSearch, SearchPatientsAsync, "ClinicSearchPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientIdentificationLookup, LookupPatientByIdentificationCodeAsync, "ClinicLookupPatientByIdentificationCode", typeof(PatientViewDto));
        _ = group.MapModulePost(ClinicApiRoutes.PatientFindOrCreate, FindOrCreatePatientAsync, "ClinicFindOrCreatePatient", typeof(PatientViewDto))
            .RequireAuthorization(SeaManagePolicy);
        _ = group.MapModuleGet(ClinicApiRoutes.PatientDetails, GetPatientDetailsAsync, "ClinicGetPatientDetails", typeof(PatientViewDto));
        _ = group.MapModulePut(ClinicApiRoutes.PatientUpdate, UpdatePatientAsync, "ClinicUpdatePatient", typeof(PatientViewDto))
            .RequireAuthorization(SeaManagePolicy);
        _ = group.MapModulePost(ClinicApiRoutes.PatientAgent, RunPatientAgentAsync, "ClinicRunPatientAgent", typeof(RunPatientAgentResponse));
        _ = group.MapModulePost(
                ClinicApiRoutes.StartVisitPrescriptionDraft,
                StartVisitPrescriptionDraftAsync,
                "ClinicStartVisitPrescriptionDraft",
                typeof(StartVisitPrescriptionDraftResponse))
            .RequireAuthorization(SeaManagePolicy);

        return endpoints;
    }

    private static async Task<IResult> RunPatientAgentAsync(
        Guid patientId,
        RunPatientAgentRequest request,
        [FromServices] IPatientAgentContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
    {
        if (patientId == Guid.Empty || request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Patient id and message are required."));
        }

        PatientAgentContext context = await contextFactory.CreateAsync(patientId, request.ConversationId, ct);
        PatientAgentResult result = await sender.Send(new RunPatientAgent(context, new PatientAgentInput(request.Message)), ct);
        return Results.Ok(new RunPatientAgentResponse(
            result.AgentRunId,
            result.ConversationId,
            result.Messages.Select(message => new PatientAgentMessageResponse(
                (PatientAgentMessageResponseKind)(int)message.Kind,
                message.Content)).ToArray(),
            result.Actions.Select(action => new PatientAgentActionResponse(
                action.Id,
                action.Capability,
                action.ActionType,
                (PatientAgentActionResponseStatus)(int)action.Status,
                System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(action.ArgumentsJson),
                action.Summary)).ToArray()));
    }

    private static async Task<IResult> StartVisitPrescriptionDraftAsync(
        StartVisitPrescriptionDraftRequest request,
        ISender sender,
        CancellationToken ct)
    {
        StartVisitPrescriptionDraftResult result = await sender.Send(
            new StartVisitPrescriptionDraftCommand(
                request.PatientId,
                request.ProviderKey,
                request.EventTitle,
                request.IdempotencyKey),
            ct);
        return result.Outcome switch
        {
            StartVisitPrescriptionDraftOutcome.Succeeded => Results.Ok(
                new StartVisitPrescriptionDraftResponse(result.EventId!.Value, result.PrescriptionId!.Value)),
            StartVisitPrescriptionDraftOutcome.ValidationFailure => Results.BadRequest(
                new ClinicError(ClinicErrorCode.ValidationFailure, result.Message ?? "The request is invalid.")),
            StartVisitPrescriptionDraftOutcome.PatientNotFound => Results.NotFound(
                new ClinicError(ClinicErrorCode.NotFound, "Patient was not found.")),
            StartVisitPrescriptionDraftOutcome.InProgress => Results.Conflict(
                new ClinicError(ClinicErrorCode.Conflict, "The visit prescription draft is already being started.")),
            StartVisitPrescriptionDraftOutcome.RecoverablePartialFailure => Results.Conflict(
                new ClinicError(ClinicErrorCode.Conflict, result.Message ?? "The visit was created but the prescription draft requires recovery.")),
            StartVisitPrescriptionDraftOutcome.UnknownMutationOutcome => Results.Conflict(
                new ClinicError(ClinicErrorCode.Conflict, "Visit creation has an unknown outcome and was not retried automatically.")),
            _ => Results.Json(
                new ClinicError(ClinicErrorCode.UnexpectedFailure, "The visit prescription draft could not be started."),
                statusCode: StatusCodes.Status503ServiceUnavailable)
        };
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
        FindOrCreatePatientResult result = await sender.Send(
            new FindOrCreatePatientCommand(
                request.IdentificationCode,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Email,
                request.DateOfBirth,
                request.DateOfSurgery,
                request.Gender is null ? null : (DomainGender)(int)request.Gender.Value,
                request.PlaceOfBirth,
                request.Address,
                request.Education is null ? null : (DomainEducation)(int)request.Education.Value,
                request.Occupation,
                request.FatherName,
                request.Height,
                request.Weight,
                request.Referrer,
                request.InsuranceProvider,
                request.VisitReason,
                request.Allergies,
                request.CurrentMedications,
                request.ClinicalNotes,
                request.EmergencyContactName,
                request.EmergencyContactPhone),
            ct);

        if (result is FindOrCreatePatientResult.Success success)
        {
            return Results.Ok(ToContract(success.Patient));
        }

        FindOrCreatePatientResult.Failure failure = (FindOrCreatePatientResult.Failure)result;
        return failure.Reason switch
        {
            PatientCreationFailureReason.Validation => Results.BadRequest(new ClinicError(
                ClinicErrorCode.ValidationFailure,
                failure.Message,
                failure.ValidationErrors)),
            PatientCreationFailureReason.Conflict => Results.Conflict(new ClinicError(
                ClinicErrorCode.Conflict,
                "A patient with this identification code already exists.")),
            _ => Results.Json(
                new ClinicError(ClinicErrorCode.UnexpectedFailure, "Patient creation failed."),
                statusCode: StatusCodes.Status500InternalServerError)
        };
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

    private static async Task<IResult> UpdatePatientAsync(Guid patientId, PatientUpdateRequest request, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Patient id is required."));
        }

        if (request is null)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Request body is required."));
        }

        PatientUpdateResult result = await sender.Send(
            new UpdatePatientCommand(
                patientId,
                request.IdentificationCode,
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Email,
                request.DateOfBirth,
                request.DateOfSurgery,
                request.Gender is null ? null : (DomainGender)(int)request.Gender.Value,
                request.PlaceOfBirth,
                request.Address,
                request.Education is null ? null : (DomainEducation)(int)request.Education.Value,
                request.Occupation,
                request.FatherName,
                request.Height,
                request.Weight,
                request.Referrer,
                request.InsuranceProvider,
                request.VisitReason,
                request.Allergies,
                request.CurrentMedications,
                request.ClinicalNotes,
                request.EmergencyContactName,
                request.EmergencyContactPhone),
            ct);

        return result switch
        {
            PatientUpdateResult.Success success => Results.Ok(ToContract(success.Patient)),
            PatientUpdateResult.NotFound => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient was not found.")),
            PatientUpdateResult.Failure { Reason: PatientUpdateFailureReason.Validation } failure => Results.BadRequest(new ClinicError(
                ClinicErrorCode.ValidationFailure,
                failure.Message,
                failure.ValidationErrors)),
            PatientUpdateResult.Failure { Reason: PatientUpdateFailureReason.Conflict } => Results.Conflict(new ClinicError(
                ClinicErrorCode.Conflict,
                "A patient with this identification code already exists.")),
            _ => Results.Json(
                new ClinicError(ClinicErrorCode.UnexpectedFailure, "Patient update failed."),
                statusCode: StatusCodes.Status500InternalServerError)
        };
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
