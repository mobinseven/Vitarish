using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Commands.Visits;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Application.Queries.Dashboard;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Capabilities;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Api.Assistant;
using Vitarish.Clinic.Contracts.Authorization;
using Vitarish.Clinic.Contracts.Errors;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Clinic.Contracts;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Contracts.Authorization;
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
    private const string SurgeryOperationLogCategory = "Vitarish.Clinic.Surgeries";

    public static IEndpointRouteBuilder MapClinicApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapProtectedModuleGroup(ClinicApiRoutes.Base);
        RouteGroupBuilder internalGroup = endpoints.MapInternalServiceModuleGroup(string.Empty);

        _ = group.MapModuleGet(ClinicApiRoutes.Health, () => Results.Ok(), "ClinicGetHealth");
        _ = group.MapModulePost(ClinicApiRoutes.Patients, ListPatientsAsync, "ClinicListPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientCount, CountPatientsAsync, "ClinicCountPatients", typeof(int));
        _ = group.MapModulePost(ClinicApiRoutes.PatientSearch, SearchPatientsAsync, "ClinicSearchPatients", typeof(PatientViewDto[]));
        _ = group.MapModulePost(ClinicApiRoutes.PatientIdentificationLookup, LookupPatientByIdentificationCodeAsync, "ClinicLookupPatientByIdentificationCode", typeof(PatientViewDto));
        _ = group.MapModulePost(ClinicApiRoutes.PatientFindOrCreate, FindOrCreatePatientAsync, "ClinicFindOrCreatePatient", typeof(PatientViewDto))
            .RequireAuthorization(SeaAuthorizationPolicies.CreateProject);
        _ = group.MapModuleGet(ClinicApiRoutes.PatientDetails, GetPatientDetailsAsync, "ClinicGetPatientDetails", typeof(PatientViewDto));
        _ = group.MapModulePut(ClinicApiRoutes.PatientUpdate, UpdatePatientAsync, "ClinicUpdatePatient", typeof(PatientViewDto))
            .RequireAuthorization(SeaAuthorizationPolicies.UpdateProject);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgeryCatalogs, GetSurgeryCatalogsAsync, "ClinicGetSurgeryCatalogs", typeof(SurgeryCatalogDto[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryCatalogs, CreateSurgeryCatalogAsync, "ClinicCreateSurgeryCatalog", typeof(SurgeryCatalogDto), successStatusCode: StatusCodes.Status201Created)
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePut(ClinicApiRoutes.SurgeryCatalogById, UpdateSurgeryCatalogAsync, "ClinicUpdateSurgeryCatalog", typeof(SurgeryCatalogDto))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryCatalogDelete, DeleteSurgeryCatalogAsync, "ClinicDeleteSurgeryCatalog", successStatusCode: StatusCodes.Status204NoContent)
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryDefaultCatalog, EnsureDefaultSurgeryCatalogAsync, "ClinicEnsureDefaultSurgeryCatalog", typeof(SurgeryCatalogDto))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgerySettings, GetSurgerySettingsAsync, "ClinicGetSurgerySettings", typeof(SurgerySettingsDto))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModulePut(ClinicApiRoutes.SurgerySettings, UpdateSurgerySettingsAsync, "ClinicUpdateSurgerySettings", typeof(SurgerySettingsDto))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryDefaultSettings, EnsureDefaultSurgerySettingsAsync, "ClinicEnsureDefaultSurgerySettings", typeof(SurgerySettingsDto))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModuleGet(ClinicApiRoutes.Surgeries, ListSurgeriesAsync, "ClinicListSurgeries", typeof(SurgeryResponse[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgeryCalendar, ListSurgeryCalendarAsync, "ClinicListSurgeryCalendar", typeof(SurgeryCalendarEntryResponse[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgeryConflicts, FindSurgeryConflictsAsync, "ClinicFindSurgeryConflicts", typeof(SurgeryConflictResponse[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgerySuggestions, GetSurgerySuggestionsAsync, "ClinicGetSurgerySuggestions", typeof(SurgerySuggestionsResponse))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.SurgeryById, GetSurgeryAsync, "ClinicGetSurgery", typeof(SurgeryCalendarEntryResponse))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.DashboardAppointments, ListDashboardAppointmentsAsync, "ClinicListDashboardAppointments", typeof(DashboardAppointmentResponse[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModuleGet(ClinicApiRoutes.PatientSurgeries, ListPatientSurgeriesAsync, "ClinicListPatientSurgeries", typeof(SurgeryResponse[]))
            .RequireAuthorization(SeaAuthorizationPolicies.Read);
        _ = group.MapModulePost(ClinicApiRoutes.PatientSurgeries, CreatePatientSurgeryAsync, "ClinicCreatePatientSurgery", typeof(SurgeryResponse))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePut(ClinicApiRoutes.SurgeryById, UpdateSurgeryAsync, "ClinicUpdateSurgery", typeof(SurgeryResponse))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryCancel, CancelSurgeryAsync, "ClinicCancelSurgery", typeof(SurgeryResponse))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.SurgeryComplete, CompleteSurgeryAsync, "ClinicCompleteSurgery", typeof(SurgeryResponse))
            .RequireAuthorization(ClinicAuthorizationPolicies.PlanSurgeries);
        _ = group.MapModulePost(ClinicApiRoutes.PatientAssistant, RunCanonicalPatientAssistantAsync, "ClinicRunPatientAssistant", typeof(RunClinicAssistantResponse));
        _ = group.MapModulePost(ClinicApiRoutes.LegacyPatientAgent, RunLegacyPatientAgentAsync, "ClinicRunPatientAgent", typeof(RunClinicAssistantResponse));
        _ = group.MapModuleDelete(ClinicApiRoutes.PatientAssistantConversation, DeleteCanonicalPatientAssistantConversationAsync, "ClinicDeletePatientAssistantConversation");
        _ = group.MapModuleDelete(ClinicApiRoutes.LegacyPatientAgent + "/{conversationId:guid}", DeleteLegacyPatientAgentConversationAsync, "ClinicDeletePatientAgentConversation");
        _ = group.MapModuleGet(ClinicApiRoutes.PatientAssistantAction, GetPatientAssistantActionAsync, "ClinicGetPatientAssistantAction", typeof(ClinicAssistantActionDetailsResponse));
        _ = group.MapModuleGet(ClinicApiRoutes.LegacyPatientAgentAction, GetPatientAssistantActionAsync, "ClinicGetPatientAgentAction", typeof(ClinicAssistantActionDetailsResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ConfirmPatientAssistantAction, ConfirmPatientAssistantActionAsync, "ClinicConfirmPatientAssistantAction", typeof(ConfirmClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ConfirmLegacyPatientAgentAction, ConfirmPatientAssistantActionAsync, "ClinicConfirmPatientAgentAction", typeof(ConfirmClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.RejectPatientAssistantAction, RejectPatientAssistantActionAsync, "ClinicRejectPatientAssistantAction", typeof(RejectClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.RejectLegacyPatientAgentAction, RejectPatientAssistantActionAsync, "ClinicRejectPatientAgentAction", typeof(RejectClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.OperationsAssistant, RunCanonicalOperationsAssistantAsync, "ClinicRunOperationsAssistant", typeof(RunClinicAssistantResponse));
        _ = group.MapModulePost(ClinicApiRoutes.LegacyOperationsAgent, RunLegacyOperationsAgentAsync, "ClinicRunOperationsAgent", typeof(RunClinicAssistantResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ResolveClinicAssistantCandidate, ResolveClinicAssistantCandidateAsync, "ClinicResolveClinicAssistantCandidate", typeof(ResolveClinicAssistantCandidateResponse));
        _ = group.MapModulePost(ClinicApiRoutes.LegacyResolvePatientAgentCandidate, ResolveClinicAssistantCandidateAsync, "ClinicResolvePatientAgentCandidate", typeof(ResolveClinicAssistantCandidateResponse));
        _ = group.MapModuleDelete(ClinicApiRoutes.OperationsAssistantConversation, DeleteCanonicalOperationsAssistantConversationAsync, "ClinicDeleteOperationsAssistantConversation");
        _ = group.MapModuleDelete(ClinicApiRoutes.LegacyOperationsAgent + "/{conversationId:guid}", DeleteLegacyOperationsAgentConversationAsync, "ClinicDeleteOperationsAgentConversation");
        _ = group.MapModuleGet(ClinicApiRoutes.AssistantAction, GetOperationsAssistantActionAsync, "ClinicGetAssistantAction", typeof(ClinicAssistantActionDetailsResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ConfirmAssistantAction, ConfirmOperationsAssistantActionAsync, "ClinicConfirmAssistantAction", typeof(ConfirmClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.RejectAssistantAction, RejectOperationsAssistantActionAsync, "ClinicRejectAssistantAction", typeof(RejectClinicAssistantActionResponse));
        _ = group.MapModuleGet(ClinicApiRoutes.LegacyAgentActions, GetOperationsAssistantActionAsync, "ClinicGetAgentAction", typeof(ClinicAssistantActionDetailsResponse));
        _ = group.MapModulePost(ClinicApiRoutes.LegacyAgentActions + "/confirm", ConfirmOperationsAssistantActionAsync, "ClinicConfirmAgentAction", typeof(ConfirmClinicAssistantActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.LegacyAgentActions + "/reject", RejectOperationsAssistantActionAsync, "ClinicRejectAgentAction", typeof(RejectClinicAssistantActionResponse));
        _ = internalGroup.MapModulePost(
                ClinicApiRoutes.InternalAssistantTools,
                ExecuteClinicAssistantToolAsync,
                "ClinicExecuteInternalAssistantTool",
                typeof(ExecuteClinicAssistantToolResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: "The endpoint is an internal callback from the server-side Agents SDK runtime and validates a signed, short-lived execution credential.")
            .ExcludeFromOpenApiWithJustification("Internal agent runtime callback; not a public module API.");
        _ = internalGroup.MapModulePost(
                ClinicApiRoutes.LegacyInternalAgentTools,
                ExecuteClinicAssistantToolAsync,
                "ClinicExecuteInternalAgentTool",
                typeof(ExecuteClinicAssistantToolResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: "Legacy internal agent callback retained during the assistant route migration.")
            .ExcludeFromOpenApiWithJustification("Legacy internal agent runtime callback retained during the assistant route migration.");
        _ = internalGroup.MapModuleGet(
                ClinicApiRoutes.InternalAssistantHealth,
                AgentRuntimeHealthAsync,
                "ClinicGetInternalAssistantHealth",
                typeof(object),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: "Internal agent runtime connectivity probe; it is protected by the internal service boundary.")
            .ExcludeFromOpenApiWithJustification("Internal agent runtime connectivity probe; not a public module API.");
        _ = internalGroup.MapModuleGet(
                ClinicApiRoutes.LegacyInternalAgentHealth,
                AgentRuntimeHealthAsync,
                "ClinicGetInternalAgentHealth",
                typeof(object),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: "Legacy internal agent runtime connectivity probe retained behind the internal service boundary.")
            .ExcludeFromOpenApiWithJustification("Legacy internal agent runtime connectivity probe retained during the assistant route migration.");
        _ = group.MapModulePost(
                ClinicApiRoutes.StartVisitPrescriptionDraft,
                StartVisitPrescriptionDraftAsync,
                "ClinicStartVisitPrescriptionDraft",
                typeof(StartVisitPrescriptionDraftResponse))
            .RequireAuthorization(SeaAuthorizationPolicies.CreateProject);

        return endpoints;
    }

    private static async Task<IResult> GetSurgeryCatalogsAsync([FromServices] ISurgeryCatalogStore catalogs, CancellationToken cancellationToken)
        => Results.Ok((await catalogs.ListAsync(cancellationToken)).Select(ToContract).ToArray());

    private static Task<IResult> CreateSurgeryCatalogAsync(HttpContext httpContext, SurgeryCatalogWriteRequest? request, [FromServices] ISurgeryCatalogStore catalogs, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgeryCatalogWriteRequest validRequest = ValidateCatalogRequestShape(request);
            SurgeryCatalog created = await catalogs.CreateAsync(ToDomain(validRequest), cancellationToken);
            return Results.Created($"{ClinicApiRoutes.Base}{ClinicApiRoutes.SurgeryCatalogs}/{created.Id}", ToContract(created));
        });

    private static Task<IResult> UpdateSurgeryCatalogAsync(HttpContext httpContext, Guid id, SurgeryCatalogUpdateRequest? request, [FromServices] ISurgeryCatalogStore catalogs, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgeryCatalogUpdateRequest validRequest = ValidateCatalogRequestShape(request);
            if (validRequest.ConcurrencyToken == Guid.Empty)
            {
                return Results.BadRequest("A concurrency token is required.");
            }

            SurgeryCatalog? result = await catalogs.UpdateAsync(id, ToDomain(validRequest), validRequest.ConcurrencyToken, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
        });

    private static Task<IResult> DeleteSurgeryCatalogAsync(HttpContext httpContext, Guid id, SurgeryCatalogDeleteRequest? request, [FromServices] ISurgeryCatalogStore catalogs, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            if (request is null || request.ConcurrencyToken == Guid.Empty)
            {
                return Results.BadRequest("A concurrency token is required.");
            }

            bool deleted = await catalogs.DeleteAsync(id, request.ConcurrencyToken, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

    private static Task<IResult> EnsureDefaultSurgeryCatalogAsync(HttpContext httpContext, SurgeryCatalogWriteRequest? request, [FromServices] ISurgeryCatalogStore catalogs, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgeryCatalogWriteRequest validRequest = ValidateCatalogRequestShape(request);
            SurgeryCatalog result = await catalogs.EnsureDefaultAsync(ToDomain(validRequest), cancellationToken);
            return Results.Ok(ToContract(result));
        });

    private static async Task<IResult> GetSurgerySettingsAsync([FromServices] ISurgerySettingsStore settings, CancellationToken cancellationToken)
    {
        SurgerySettings? result = await settings.GetAsync(cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
    }

    private static Task<IResult> UpdateSurgerySettingsAsync(HttpContext httpContext, SurgerySettingsUpdateRequest? request, [FromServices] ISurgerySettingsStore settings, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgerySettingsUpdateRequest validRequest = ValidateSettingsRequestShape(request);
            if (validRequest.ConcurrencyToken == Guid.Empty)
            {
                return Results.BadRequest("A concurrency token is required.");
            }

            SurgerySettings? result = await settings.UpdateAsync(ToDomain(validRequest), validRequest.ConcurrencyToken, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
        });

    private static Task<IResult> EnsureDefaultSurgerySettingsAsync(HttpContext httpContext, SurgerySettingsWriteRequest? request, [FromServices] ISurgerySettingsStore settings, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgerySettingsWriteRequest validRequest = ValidateSettingsRequestShape(request);
            SurgerySettings result = await settings.EnsureDefaultAsync(ToDomain(validRequest), cancellationToken);
            return Results.Ok(ToContract(result));
        });

    private static async Task<IResult> ListSurgeriesAsync([FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => Results.Ok((await toolchain.GetUpcomingAsync(cancellationToken)).Select(ToContract).ToArray());

    private static async Task<IResult> ListSurgeryCalendarAsync([AsParameters] SurgeryCalendarRequest request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
    {
        if (request.to <= request.from)
        {
            return Results.BadRequest("The calendar end must be after its start.");
        }

        if (request.to - request.from > TimeSpan.FromDays(SurgeryCalendarLimits.MaxRangeDays))
        {
            return Results.BadRequest($"The calendar range cannot exceed {SurgeryCalendarLimits.MaxRangeDays} days.");
        }

        return Results.Ok((await toolchain.GetCalendarAsync(request.from, request.to, cancellationToken)).Select(ToContract).ToArray());
    }

    private static async Task<IResult> FindSurgeryConflictsAsync([AsParameters] SurgeryConflictRequest request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
    {
        SurgeryConflictProbe probe = new(request.scheduledAt, request.durationMinutes, request.location, request.teamUserIds ?? [], request.excludeSurgeryId);
        return Results.Ok((await toolchain.FindConflictsAsync(probe, cancellationToken)).Select(ToContract).ToArray());
    }

    private static async Task<IResult> GetSurgerySuggestionsAsync([FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
    {
        SurgerySuggestions suggestions = await toolchain.GetSuggestionsAsync(cancellationToken);
        return Results.Ok(new SurgerySuggestionsResponse(suggestions.Locations, suggestions.Roles));
    }

    private static async Task<IResult> GetSurgeryAsync(Guid id, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
    {
        SurgeryCalendarEntry? entry = await toolchain.GetAsync(id, cancellationToken);
        return entry is null ? Results.NotFound() : Results.Ok(ToContract(entry));
    }

    private static Task<IResult> ListDashboardAppointmentsAsync([AsParameters] DashboardDateRangeRequest request, [FromServices] IClinicDashboardQueries queries, CancellationToken cancellationToken)
        => ListDashboardAsync(request, queries.ListAppointmentsAsync, values => values.Select(value => new DashboardAppointmentResponse(value.ReservationId, value.PatientId, value.PatientDisplayName, value.ServiceId, value.ServiceDisplayName, value.Date, value.StartTime, value.TimeZone, (Vitarish.Appointments.Contracts.Enums.ReservationStatus)value.Status, value.PatientTags)).ToArray(), cancellationToken);

    private static async Task<IResult> ListDashboardAsync<T, TContract>(DashboardDateRangeRequest request, Func<DashboardDateRange, CancellationToken, Task<IReadOnlyList<T>>> query, Func<IReadOnlyList<T>, TContract[]> map, CancellationToken cancellationToken)
    {
        try
        {
            DashboardDateRange range = DashboardDateRange.Create(request.dateFrom, request.dateTo, request.take);
            return Results.Ok(map(await query(range, cancellationToken)));
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> ListPatientSurgeriesAsync(Guid patientId, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => Results.Ok((await toolchain.GetByPatientAsync(patientId, cancellationToken)).Select(ToContract).ToArray());

    private static Task<IResult> CreatePatientSurgeryAsync(HttpContext httpContext, Guid patientId, SurgeryCreateRequest? request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgeryCreateRequest validRequest = ValidateRequestShape(request);
            SurgeryReadModel result = await toolchain.CreateAsync(ToDomain(Guid.Empty, patientId, validRequest), cancellationToken);
            return Results.Created($"{ClinicApiRoutes.Base}{ClinicApiRoutes.Surgeries}/{result.Id}", ToContract(result));
        });

    private static Task<IResult> UpdateSurgeryAsync(HttpContext httpContext, Guid id, SurgeryWriteRequest? request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            SurgeryWriteRequest validRequest = ValidateRequestShape(request);
            SurgeryReadModel? result = await toolchain.UpdateAsync(ToDomain(id, Guid.Empty, validRequest), cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
        });

    private static Task<IResult> CancelSurgeryAsync(HttpContext httpContext, Guid id, SurgeryCancelRequest? request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            if (request is null || request.ConcurrencyToken == Guid.Empty)
            {
                return Results.BadRequest("A concurrency token is required.");
            }

            SurgeryReadModel? result = await toolchain.CancelAsync(id, request.ConcurrencyToken, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
        });

    private static Task<IResult> CompleteSurgeryAsync(HttpContext httpContext, Guid id, SurgeryCompleteRequest? request, [FromServices] ISurgeryToolchain toolchain, CancellationToken cancellationToken)
        => ExecuteSurgeryOperationAsync(httpContext, async () =>
        {
            if (request is null || request.ConcurrencyToken == Guid.Empty)
            {
                return Results.BadRequest("A concurrency token is required.");
            }

            SurgeryReadModel? result = await toolchain.CompleteAsync(id, request.ConcurrencyToken, cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(ToContract(result));
        });

    private static async Task<IResult> ExecuteSurgeryOperationAsync(HttpContext httpContext, Func<Task<IResult>> operation)
    {
        try
        {
            return await operation();
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(exception.Message);
        }
        catch (SurgeryPlanningForbiddenException)
        {
            return Results.Forbid();
        }
        catch (SurgeryOperationUnavailableException exception)
        {
            httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger(SurgeryOperationLogCategory)
                .LogError(exception, "A surgery operation failed because a dependency is unavailable.");
            return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(exception.Message);
        }
    }

    private static SurgeryWriteModel ToDomain(Guid id, Guid patientId, SurgeryWriteRequest request)
        => ToDomain(
            id,
            patientId,
            request.CatalogId,
            request.Name,
            request.Status,
            request.ScheduledAt,
            request.Tools,
            request.PersonsToNotify,
            request.Contacts,
            request.ReminderPolicies,
            request.ConcurrencyToken,
            null,
            request.DurationMinutes,
            request.Location,
            request.Team);

    private static SurgeryWriteModel ToDomain(Guid id, Guid patientId, SurgeryCreateRequest request)
        => ToDomain(
            id,
            patientId,
            request.CatalogId,
            request.Name,
            request.Status,
            request.ScheduledAt,
            request.Tools,
            request.PersonsToNotify,
            request.Contacts,
            request.ReminderPolicies,
            Guid.Empty,
            request.IdempotencyKey,
            request.DurationMinutes,
            request.Location,
            request.Team);

    private static SurgeryWriteModel ToDomain(
        Guid id,
        Guid patientId,
        string catalogId,
        string name,
        string statusValue,
        DateTimeOffset scheduledAt,
        IReadOnlyList<string> tools,
        IReadOnlyList<string> personsToNotify,
        IReadOnlyList<SurgeryContactDto> contacts,
        IReadOnlyList<SurgeryReminderPolicyDto> reminderPolicies,
        Guid concurrencyToken,
        string? idempotencyKey,
        int? durationMinutes,
        string? location,
        IReadOnlyList<SurgeryTeamMemberDto>? team)
    {
        if (!Enum.TryParse(statusValue, true, out SurgeryStatus status))
        {
            throw new ArgumentException($"Status must be one of: {string.Join(", ", Enum.GetNames<SurgeryStatus>())}.", nameof(statusValue));
        }

        return new(
            id,
            patientId,
            catalogId,
            name,
            idempotencyKey,
            status,
            scheduledAt,
            tools,
            personsToNotify,
            contacts.Select(item => new SurgeryContact(item.Name, item.Destination, item.Channel)).ToArray(),
            reminderPolicies.Select(ToDomain).ToArray(),
            concurrencyToken,
            durationMinutes,
            location,
            (team ?? []).Select(item => new SurgeryTeamMember(item.UserId, item.DisplayName, item.Role)).ToArray());
    }

    private static SurgeryCatalogWriteModel ToDomain(SurgeryCatalogWriteRequest request)
        => new(request.Name, request.Tools, request.DefaultTeamRoles, request.ReminderPolicies.Select(ToDomain).ToArray());

    private static SurgeryCatalogWriteModel ToDomain(SurgeryCatalogUpdateRequest request)
        => new(request.Name, request.Tools, request.DefaultTeamRoles, request.ReminderPolicies.Select(ToDomain).ToArray());

    private static SurgerySettingsWriteModel ToDomain(SurgerySettingsWriteRequest request)
        => new(request.CancellationSubject, request.CancellationTextBody);

    private static SurgerySettingsWriteModel ToDomain(SurgerySettingsUpdateRequest request)
        => new(request.CancellationSubject, request.CancellationTextBody);

    private static SurgeryReminderPolicy ToDomain(SurgeryReminderPolicyDto value)
        => new(value.Name, value.MinutesBefore, value.Subject, value.TextBody);

    private static SurgeryCreateRequest ValidateRequestShape(SurgeryCreateRequest? request)
    {
        if (request is null) throw new ArgumentException("A surgery request is required.");
        if (request.Tools is null || request.PersonsToNotify is null || request.Contacts is null || request.ReminderPolicies is null)
        {
            throw new ArgumentException("Surgery collections cannot be null.");
        }

        if (request.Team is not null && request.Team.Any(member => member is null || string.IsNullOrWhiteSpace(member.UserId) || string.IsNullOrWhiteSpace(member.Role)))
        {
            throw new ArgumentException("Each team member needs a user and a role.");
        }

        return request;
    }

    private static SurgeryWriteRequest ValidateRequestShape(SurgeryWriteRequest? request)
    {
        if (request is null) throw new ArgumentException("A surgery request is required.");
        if (request.Tools is null || request.PersonsToNotify is null || request.Contacts is null || request.ReminderPolicies is null)
        {
            throw new ArgumentException("Surgery collections cannot be null.");
        }

        if (request.Team is not null && request.Team.Any(member => member is null || string.IsNullOrWhiteSpace(member.UserId) || string.IsNullOrWhiteSpace(member.Role)))
        {
            throw new ArgumentException("Each team member needs a user and a role.");
        }

        return request;
    }

    private static SurgeryCatalogWriteRequest ValidateCatalogRequestShape(SurgeryCatalogWriteRequest? request)
    {
        if (request is null) throw new ArgumentException("A surgery type request is required.");
        if (request.Tools is null || request.DefaultTeamRoles is null || request.ReminderPolicies is null)
        {
            throw new ArgumentException("Surgery type collections cannot be null.");
        }

        return request;
    }

    private static SurgeryCatalogUpdateRequest ValidateCatalogRequestShape(SurgeryCatalogUpdateRequest? request)
    {
        if (request is null) throw new ArgumentException("A surgery type request is required.");
        if (request.Tools is null || request.DefaultTeamRoles is null || request.ReminderPolicies is null)
        {
            throw new ArgumentException("Surgery type collections cannot be null.");
        }

        return request;
    }

    private static SurgerySettingsWriteRequest ValidateSettingsRequestShape(SurgerySettingsWriteRequest? request)
        => request ?? throw new ArgumentException("Surgery settings are required.");

    private static SurgerySettingsUpdateRequest ValidateSettingsRequestShape(SurgerySettingsUpdateRequest? request)
        => request ?? throw new ArgumentException("Surgery settings are required.");

    private static SurgeryCatalogDto ToContract(SurgeryCatalog value)
        => new(value.Id, value.Name, value.Tools, value.DefaultTeamRoles, value.ReminderPolicies.Select(ToContract).ToArray(), value.ConcurrencyToken);

    private static SurgeryResponse ToContract(SurgeryReadModel value)
        => new(value.Id, value.PatientId, value.CatalogId, value.Name, value.IdempotencyKey, value.Status.ToString(), value.ScheduledAt, value.Tools, value.PersonsToNotify, value.Contacts.Select(item => new SurgeryContactDto(item.Name, item.Destination, item.Channel)).ToArray(), value.ReminderPolicies.Select(ToContract).ToArray(), value.ConcurrencyToken, value.CreatedAt, value.UpdatedAt, value.DurationMinutes, value.Location, value.Team.Select(item => new SurgeryTeamMemberDto(item.UserId, item.DisplayName, item.Role)).ToArray());

    private static SurgeryCalendarEntryResponse ToContract(SurgeryCalendarEntry value)
        => new(ToContract(value.Surgery), value.PatientDisplayName);

    private static SurgeryConflictResponse ToContract(SurgeryConflict value)
        => new(ToContract(value.Entry), value.Reasons.Select(reason => reason.ToString()).ToArray(), value.SharedTeamUserIds);

    private static SurgeryReminderPolicyDto ToContract(SurgeryReminderPolicy value)
        => new(value.Name, value.MinutesBefore, value.Subject, value.TextBody);

    private static SurgerySettingsDto ToContract(SurgerySettings value)
        => new(value.CancellationSubject, value.CancellationTextBody, value.ConcurrencyToken);

    private static IResult AgentRuntimeHealthAsync(
        HttpRequest request,
        [FromServices] IOptions<Vitarish.Intelligence.Domain.Options.AgentRuntimeOptions> options)
    {
        string? credential = request.Headers[InternalServiceEndpointConventions.CredentialHeaderName].FirstOrDefault();
        string expected = options.Value.SharedSecret;
        bool valid = !string.IsNullOrWhiteSpace(options.Value.SharedSecret) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(credential ?? string.Empty), Encoding.UTF8.GetBytes(expected));
        return valid ? Results.Ok(new { status = "ok", component = "intelligence-agent-runtime" }) : Results.Unauthorized();
    }

    private static async Task<IResult> ExecuteClinicAssistantToolAsync(
        ExecuteClinicAssistantToolRequest request,
        HttpRequest httpRequest,
        [FromServices] IAgentRuntimeCredentialService credentialService,
        [FromServices] IAgentRuntimeReplayGuard replayGuard,
        [FromServices] ClinicRuntimeToolEndpoint runtimeEndpoint,
        [FromServices] ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        ILogger logger = loggerFactory.CreateLogger("Vitarish.Clinic.AgentRuntimeCallback");
        string? credential = httpRequest.Headers[InternalServiceEndpointConventions.CredentialHeaderName].FirstOrDefault();
        if (request is null || request.ExecutionEnvelope is null || string.IsNullOrWhiteSpace(request.ArgumentsJson))
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "invalid_request", request?.ExecutionEnvelope?.ExecutionId ?? "", request?.ExecutionEnvelope?.AgentKey ?? "");
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "The agent tool request is invalid."));
        }
        if (!credentialService.ValidateCallback(credential ?? string.Empty, request.ExecutionEnvelope))
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "credential_or_envelope", request.ExecutionEnvelope.ExecutionId, request.ExecutionEnvelope.AgentKey);
            return Results.Unauthorized();
        }
        if (!await replayGuard.TryClaimAsync(request.ExecutionEnvelope, ct))
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "replay_or_expired", request.ExecutionEnvelope.ExecutionId, request.ExecutionEnvelope.AgentKey);
            return Results.Conflict();
        }

        try
        {
            AgentExecutionContext executionContext = request.ExecutionEnvelope.ToExecutionContext();
            AgentToolResult result = await runtimeEndpoint.ExecuteAsync(
                new(new AgentToolCall(request.ExecutionEnvelope.ToolCallId, request.ExecutionEnvelope.ToolName, request.ArgumentsJson), executionContext, request.ExecutionEnvelope), ct);
            return Results.Ok(new ExecuteClinicAssistantToolResponse(result.CallId, result.Output, result.IsError));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "scope_or_authorization", request.ExecutionEnvelope.ExecutionId, request.ExecutionEnvelope.AgentKey);
            return Results.Json(new ClinicError(ClinicErrorCode.Forbidden, "The requested agent tool is not permitted."), statusCode: StatusCodes.Status403Forbidden);
        }
        catch (KeyNotFoundException)
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "tool_not_found", request.ExecutionEnvelope.ExecutionId, request.ExecutionEnvelope.AgentKey);
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "The requested clinic-assistant tool is not available."));
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or FormatException or ArgumentException or InvalidOperationException)
        {
            AssistantCallbackTelemetry.CallbackRejected(logger, "validation", request.ExecutionEnvelope.ExecutionId, request.ExecutionEnvelope.AgentKey);
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "The agent tool arguments are invalid."));
        }
    }

    private static async Task<IResult> GetPatientAssistantActionAsync(
        Guid patientId, Guid actionId, [FromServices] IAssistantContextFactory contextFactory,
        ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty) return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateAsync(patientId, null, ct); }
        catch (KeyNotFoundException) { return NotFoundClinicAssistant(); }
        AssistantActionResult result = await sender.Send(new GetAssistantAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(ToDetails(action)));
    }

    private static async Task<IResult> GetOperationsAssistantActionAsync(Guid actionId, [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty) return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }
        AssistantActionResult result = await sender.Send(new GetAssistantAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(ToDetails(action)));
    }

    private static async Task<IResult> ConfirmOperationsAssistantActionAsync(Guid actionId, [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty) return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }
        AssistantActionResult result = await sender.Send(new ConfirmAssistantAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(new ConfirmClinicAssistantActionResponse(action.ActionId, ToContract(action.Status), Json(action.CompletionPayload), ToFailure(action.Failure))));
    }

    private static async Task<IResult> RejectOperationsAssistantActionAsync(Guid actionId, RejectClinicAssistantActionRequest request, [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty || request is null || !Guid.TryParse(request.ActionId, out Guid bodyId) || bodyId != actionId || request.Reason?.Length > 1_000) return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }
        AssistantActionResult result = await sender.Send(new RejectAssistantAction(actionId, context, request.Reason), ct);
        return MapActionResult(result, action => Results.Ok(new RejectClinicAssistantActionResponse(action.ActionId, ToContract(action.Status))));
    }

    private static async Task<IResult> RunCanonicalOperationsAssistantAsync(RunClinicAssistantRequest request, [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
        => await RunOperationsAssistantAsync(request, contextFactory, sender, ct, agentKey: null);

    private static async Task<IResult> RunLegacyOperationsAgentAsync(RunClinicAssistantRequest request, [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
        => await RunOperationsAssistantAsync(request, contextFactory, sender, ct, "clinic.operations");

    private static async Task<IResult> RunOperationsAssistantAsync(
        RunClinicAssistantRequest request,
        IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct,
        string? agentKey)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message)) return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "A message is required."));
        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(request.ConversationId, ct, agentKey); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }
        AssistantResult result = await sender.Send(new RunAssistant(context, new AssistantInput(request.Message)), ct);
        if (result.Outcome == AssistantRunOutcome.NotFound) return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."));
        if (result.Outcome == AssistantRunOutcome.Busy) return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active."));
        if (result.Outcome == AssistantRunOutcome.ProfileChanged) return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The agent profile changed; start a new conversation."));
        if (result.Outcome == AssistantRunOutcome.InvalidHistory) return Results.Conflict(new ClinicError(ClinicErrorCode.InvalidHistory, "The conversation history is invalid."));
        if (result.Outcome != AssistantRunOutcome.Succeeded) return Results.Json(new ClinicError(ClinicErrorCode.UnexpectedFailure, "The operations agent failed."), statusCode: StatusCodes.Status502BadGateway);
        return Results.Ok(ToRunResponse(result));
    }

    private static async Task<IResult> ResolveClinicAssistantCandidateAsync(
        ResolveClinicAssistantCandidateRequest request,
        [FromServices] IAssistantContextFactory contextFactory,
        [FromServices] IAssistantPatientSearch search,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.AgentRunId) || string.IsNullOrWhiteSpace(request.CandidateToken))
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "A candidate token and agent run are required."));

        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }

        AssistantCandidateReference? candidate = await search.ResolveAsync(
            context.PracticeId, context.AuthenticatedUserId, request.AgentRunId, request.CandidateToken, ct);
        return candidate is null
            ? Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "The patient candidate is invalid or expired."))
            : Results.Ok(new ResolveClinicAssistantCandidateResponse(candidate.PatientId));
    }

    private static async Task<IResult> ConfirmPatientAssistantActionAsync(
        Guid patientId, Guid actionId, ConfirmClinicAssistantActionRequest request,
        [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty || request is null ||
            !Guid.TryParse(request.ActionId, out Guid bodyActionId) || bodyActionId != actionId)
            return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateAsync(patientId, null, ct); }
        catch (KeyNotFoundException) { return NotFoundClinicAssistant(); }
        AssistantActionResult result = await sender.Send(new ConfirmAssistantAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(new ConfirmClinicAssistantActionResponse(
            action.ActionId, ToContract(action.Status), Json(action.CompletionPayload), ToFailure(action.Failure))));
    }

    private static async Task<IResult> RejectPatientAssistantActionAsync(
        Guid patientId, Guid actionId, RejectClinicAssistantActionRequest request,
        [FromServices] IAssistantContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty || request is null ||
            !Guid.TryParse(request.ActionId, out Guid bodyActionId) || bodyActionId != actionId ||
            request.Reason?.Length > 1_000)
            return InvalidActionRequest();
        AssistantContext context;
        try { context = await contextFactory.CreateAsync(patientId, null, ct); }
        catch (KeyNotFoundException) { return NotFoundClinicAssistant(); }
        AssistantActionResult result = await sender.Send(new RejectAssistantAction(actionId, context, request.Reason), ct);
        return MapActionResult(result, action => Results.Ok(new RejectClinicAssistantActionResponse(
            action.ActionId, ToContract(action.Status))));
    }

    private static IResult MapActionResult(AssistantActionResult result, Func<AssistantAction, IResult> success) =>
        result.Outcome switch
        {
            AssistantActionOutcome.Succeeded when result.Action is not null => success(result.Action),
            AssistantActionOutcome.ValidationFailure => InvalidActionRequest(),
            AssistantActionOutcome.NotFound => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Clinic assistant action was not found.")),
            AssistantActionOutcome.ScopeMismatch => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Clinic assistant action was not found.")),
            AssistantActionOutcome.PermissionDenied => Results.Json(new ClinicError(ClinicErrorCode.Forbidden, "The action is not permitted."), statusCode: StatusCodes.Status403Forbidden),
            AssistantActionOutcome.Expired => Results.Conflict(ToDetails(result.Action!)),
            AssistantActionOutcome.Conflict => Results.Conflict(result.Action is null ? new ClinicError(ClinicErrorCode.Conflict, "The action cannot be changed.") : ToDetails(result.Action)),
            AssistantActionOutcome.Failed => Results.Json(ToDetails(result.Action!), statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Results.Json(new ClinicError(ClinicErrorCode.UnexpectedFailure, "The action operation failed."), statusCode: StatusCodes.Status500InternalServerError)
        };

    private static ClinicAssistantActionDetailsResponse ToDetails(AssistantAction action) => new(
        action.ActionId, action.AgentRunId, action.ConversationId, action.Capability, action.ActionType,
        ToContract(action.Status), Json(action.ReviewPayload)!.Value, action.ExpiresAt,
        Json(action.CompletionPayload), ToFailure(action.Failure));

    private static Contracts.ClinicAssistant.ClinicAssistantActionStatus ToContract(Application.Assistant.AssistantActionStatus status) =>
        (Contracts.ClinicAssistant.ClinicAssistantActionStatus)(int)status;

    private static ClinicAssistantActionFailureResponse? ToFailure(AssistantActionFailure? failure) =>
        failure is null ? null : new(failure.Code, failure.Message, failure.Retryable);

    private static System.Text.Json.JsonElement? Json(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(value);

    private static IResult InvalidActionRequest() => Results.BadRequest(
        new ClinicError(ClinicErrorCode.ValidationFailure, "A valid action identifier is required."));

    private static IResult ForbiddenAssistantAction() => Results.Json(
        new ClinicError(ClinicErrorCode.Forbidden, "The operations agent is not permitted."), statusCode: StatusCodes.Status403Forbidden);

    private static IResult NotFoundClinicAssistant() => Results.NotFound(
        new ClinicError(ClinicErrorCode.NotFound, "The route-bound patient is unavailable."));

    private static RunClinicAssistantResponse ToRunResponse(AssistantResult result) => new(
        result.AgentRunId,
        result.ConversationId,
        null,
        result.Messages.Select(message => new ClinicAssistantMessageResponse(
            (ClinicAssistantMessageResponseKind)(int)message.Kind, message.Content)).ToArray(),
        result.Actions.Select(action => new ClinicAssistantActionResponse(
            action.ActionId.ToString("D"), action.Capability, action.ActionType,
            ToContract(action.Status), Json(action.ReviewPayload)!.Value, action.Summary)).ToArray(),
        result.Candidates?.Select(candidate => new ClinicAssistantCandidateResponse(
            candidate.CandidateToken, candidate.DisplayName, candidate.DateOfBirth,
            candidate.MaskedIdentification, candidate.MaskedPhone)).ToArray(),
        result.HasMoreCandidates);

    private static async Task<IResult> RunCanonicalPatientAssistantAsync(
        Guid patientId,
        RunClinicAssistantRequest request,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await RunClinicAssistantAsync(patientId, request, contextFactory, sender, ct, agentKey: null);

    private static async Task<IResult> RunLegacyPatientAgentAsync(
        Guid patientId,
        RunClinicAssistantRequest request,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await RunClinicAssistantAsync(patientId, request, contextFactory, sender, ct, "clinic.patient");

    private static async Task<IResult> RunClinicAssistantAsync(
        Guid patientId,
        RunClinicAssistantRequest request,
        IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct,
        string? agentKey)
    {
        if (patientId == Guid.Empty || request is null || string.IsNullOrWhiteSpace(request.Message))
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "Patient id and message are required."));
        }

        AssistantContext context;
        try { context = await contextFactory.CreateAsync(patientId, request.ConversationId, ct, agentKey); }
        catch (KeyNotFoundException) { return NotFoundClinicAssistant(); }
        AssistantResult result = await sender.Send(new RunAssistant(context, new AssistantInput(request.Message)), ct);
        if (result.Outcome == AssistantRunOutcome.NotFound)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Clinic assistant conversation was not found."));
        if (result.Outcome == AssistantRunOutcome.Busy)
            return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active."));
        if (result.Outcome == AssistantRunOutcome.ProfileChanged)
            return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The agent profile changed; start a new conversation."));
        if (result.Outcome == AssistantRunOutcome.InvalidHistory)
            return Results.Conflict(new ClinicError(ClinicErrorCode.InvalidHistory, "The conversation history is invalid."));
        if (result.Outcome != AssistantRunOutcome.Succeeded)
            return Results.Json(new ClinicError(ClinicErrorCode.UnexpectedFailure, "The clinic assistant failed."), statusCode: StatusCodes.Status502BadGateway);
        return Results.Ok(ToRunResponse(result));
    }

    private static async Task<IResult> DeleteCanonicalOperationsAssistantConversationAsync(
        Guid conversationId,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await DeleteOperationsAssistantConversationAsync(conversationId, contextFactory, sender, ct, agentKey: null);

    private static async Task<IResult> DeleteLegacyOperationsAgentConversationAsync(
        Guid conversationId,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await DeleteOperationsAssistantConversationAsync(conversationId, contextFactory, sender, ct, "clinic.operations");

    private static async Task<IResult> DeleteOperationsAssistantConversationAsync(
        Guid conversationId,
        IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct,
        string? agentKey)
    {
        if (conversationId == Guid.Empty)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."));

        AssistantContext context;
        try { context = await contextFactory.CreateOperationsAsync(conversationId.ToString("N"), ct, agentKey); }
        catch (UnauthorizedAccessException) { return ForbiddenAssistantAction(); }

        AgentConversationOutcome outcome = await sender.Send(new DeleteAgentConversation(
            conversationId,
            new AgentConversationScope(
                context.PracticeId,
                context.AuthenticatedUserId,
                agentKey ?? "clinic.assistant.operations",
                "practice",
                agentKey is null ? context.PracticeId : "practice")), ct);
        return outcome switch
        {
            AgentConversationOutcome.Succeeded => Results.NoContent(),
            AgentConversationOutcome.Busy => Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active.")),
            _ => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."))
        };
    }

    private static async Task<IResult> DeleteCanonicalPatientAssistantConversationAsync(
        Guid patientId,
        Guid conversationId,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await DeletePatientAssistantConversationAsync(patientId, conversationId, contextFactory, sender, ct, agentKey: null);

    private static async Task<IResult> DeleteLegacyPatientAgentConversationAsync(
        Guid patientId,
        Guid conversationId,
        [FromServices] IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
        => await DeletePatientAssistantConversationAsync(patientId, conversationId, contextFactory, sender, ct, "clinic.patient");

    private static async Task<IResult> DeletePatientAssistantConversationAsync(
        Guid patientId,
        Guid conversationId,
        IAssistantContextFactory contextFactory,
        ISender sender,
        CancellationToken ct,
        string? agentKey)
    {
        if (patientId == Guid.Empty || conversationId == Guid.Empty)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Clinic assistant conversation was not found."));

        AssistantContext context;
        try { context = await contextFactory.CreateAsync(patientId, conversationId.ToString("N"), ct, agentKey); }
        catch (KeyNotFoundException) { return NotFoundClinicAssistant(); }
        AgentConversationOutcome outcome = await sender.Send(new DeleteAgentConversation(
            conversationId,
            new AgentConversationScope(
                context.PracticeId,
                context.AuthenticatedUserId,
                agentKey ?? "clinic.assistant.patient",
                "patient",
                context.PatientId.ToString("D"))), ct);
        return outcome switch
        {
            AgentConversationOutcome.Succeeded => Results.NoContent(),
            AgentConversationOutcome.Busy => Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active.")),
            _ => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Clinic assistant conversation was not found."))
        };
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
                request.Descending,
                request.Tags),
            ct);

        return Results.Ok(patients.Select(ToContract).ToList());
    }

    private static async Task<IResult> CountPatientsAsync(PatientCountRequest request, ISender sender, CancellationToken ct)
    {
        int count = await sender.Send(new CountPatientsQuery(request.Search, request.Template, request.Tags), ct);
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
                request.EmergencyContactPhone,
                request.Tags),
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
            PatientCreationFailureReason.ReadModelUnavailable => Results.Json(
                new ClinicError(ClinicErrorCode.TransportFailure, failure.Message),
                statusCode: StatusCodes.Status503ServiceUnavailable),
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
                request.EmergencyContactPhone,
                request.Tags),
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
            PatientUpdateResult.Failure { Reason: PatientUpdateFailureReason.ReadModelUnavailable } failure => Results.Json(
                new ClinicError(ClinicErrorCode.TransportFailure, failure.Message),
                statusCode: StatusCodes.Status503ServiceUnavailable),
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
        ContractPatientSortBy.DateOfSurgery => DomainPatientSortBy.NextSurgery,
        ContractPatientSortBy.LastSurgery => DomainPatientSortBy.LastSurgery,
        ContractPatientSortBy.NextSurgery => DomainPatientSortBy.NextSurgery,
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
        Tags = patient.Tags,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
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
        Stats = ToContract(patient.Stats),
        LastSurgeryAt = patient.LastSurgeryAt,
        NextSurgeryAt = patient.NextSurgeryAt
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

internal sealed record ExecuteClinicAssistantToolRequest(
    AgentRuntimeExecutionEnvelope ExecutionEnvelope,
    string ArgumentsJson);
