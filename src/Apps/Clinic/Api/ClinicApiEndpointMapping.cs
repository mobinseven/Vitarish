using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Commands.Visits;
using Vitarish.Clinic.Application.Queries.Patients;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Application.PatientAgent.Actions;
using Vitarish.Clinic.Application.PatientAgent.Capabilities;
using Vitarish.Clinic.Application.PatientAgent.Operations;
using Vitarish.Clinic.Api.PatientAgent;
using Vitarish.Clinic.Contracts.Errors;
using Vitarish.Clinic.Contracts.PatientAgent;
using Vitarish.Clinic.Contracts.Responses;
using Vitarish.Clinic.Contracts.Routes;
using Vitarish.Clinic.Domain.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
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
        RouteGroupBuilder internalGroup = endpoints.MapAnonymousModuleGroup(
            ClinicApiRoutes.Base,
            "The internal agent tool endpoint is reachable only from the configured server-side Agents SDK runtime and authenticates every request with a signed execution credential.");

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
        // Compatibility window: new callers must use IntelligenceApiRoutes.AgentConversation.
        _ = group.MapModulePost(ClinicApiRoutes.PatientAgent, RunPatientAgentAsync, "ClinicRunPatientAgent", typeof(RunPatientAgentResponse));
        _ = group.MapModuleDelete(ClinicApiRoutes.PatientAgentConversation, DeletePatientAgentConversationAsync, "ClinicDeletePatientAgentConversation");
        _ = group.MapModuleGet(ClinicApiRoutes.PatientAgentAction, GetPatientAgentActionAsync, "ClinicGetPatientAgentAction", typeof(PatientAgentActionDetailsResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ConfirmPatientAgentAction, ConfirmPatientAgentActionAsync, "ClinicConfirmPatientAgentAction", typeof(ConfirmPatientAgentActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.RejectPatientAgentAction, RejectPatientAgentActionAsync, "ClinicRejectPatientAgentAction", typeof(RejectPatientAgentActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.OperationsAgent, RunOperationsAgentAsync, "ClinicRunOperationsAgent", typeof(RunPatientAgentResponse));
        _ = group.MapModuleDelete(ClinicApiRoutes.OperationsAgentConversation, DeleteOperationsAgentConversationAsync, "ClinicDeleteOperationsAgentConversation");
        _ = group.MapModuleGet(ClinicApiRoutes.AgentAction, GetOperationsAgentActionAsync, "ClinicGetAgentAction", typeof(PatientAgentActionDetailsResponse));
        _ = group.MapModulePost(ClinicApiRoutes.ConfirmAgentAction, ConfirmOperationsAgentActionAsync, "ClinicConfirmAgentAction", typeof(ConfirmPatientAgentActionResponse));
        _ = group.MapModulePost(ClinicApiRoutes.RejectAgentAction, RejectOperationsAgentActionAsync, "ClinicRejectAgentAction", typeof(RejectPatientAgentActionResponse));
        // Compatibility alias: configured runtimes use IntelligenceApiRoutes.InternalAgentToolExecution;
        // retain this route while older deployments still target the Clinic callback.
        _ = internalGroup.MapModulePost(
                "/internal/agent/tools/execute",
                ExecutePatientAgentToolAsync,
                "ClinicExecuteInternalPatientAgentTool",
                typeof(ExecutePatientAgentToolResponse),
                profile: ModuleEndpointProfile.AnonymousJson,
                anonymousJustification: "The endpoint is an internal callback from the server-side Agents SDK runtime and validates a signed, short-lived execution credential.")
            .ExcludeFromOpenApiWithJustification("Internal agent runtime callback; not a public module API.");
        _ = internalGroup.MapModuleGet(
                "/internal/agent/health",
                AgentRuntimeHealthAsync,
                "ClinicGetInternalAgentHealth",
                typeof(object))
            .ExcludeFromOpenApiWithJustification("Internal agent runtime connectivity probe; not a public module API.");
        _ = group.MapModulePost(
                ClinicApiRoutes.StartVisitPrescriptionDraft,
                StartVisitPrescriptionDraftAsync,
                "ClinicStartVisitPrescriptionDraft",
                typeof(StartVisitPrescriptionDraftResponse))
            .RequireAuthorization(SeaManagePolicy);

        return endpoints;
    }

    private static IResult AgentRuntimeHealthAsync(
        HttpRequest request,
        [FromServices] IOptions<Vitarish.Intelligence.Domain.Options.AgentRuntimeOptions> options)
    {
        string? authorization = request.Headers.Authorization.FirstOrDefault();
        string expected = $"Bearer {options.Value.SharedSecret}";
        bool valid = !string.IsNullOrWhiteSpace(options.Value.SharedSecret) &&
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(authorization ?? string.Empty), Encoding.UTF8.GetBytes(expected));
        return valid ? Results.Ok(new { status = "ok", component = "intelligence-agent-runtime" }) : Results.Unauthorized();
    }

    private static async Task<IResult> ExecutePatientAgentToolAsync(
        ExecutePatientAgentToolRequest request,
        HttpRequest httpRequest,
        [FromServices] IAgentRuntimeCredentialService credentialService,
        [FromServices] ClinicRuntimeToolEndpoint runtimeEndpoint,
        CancellationToken ct)
    {
        string? credential = httpRequest.Headers["X-Vitarish-Agent-Credential"].FirstOrDefault();
        if (request is null) return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "The agent tool request is required."));
        if (request.Metadata is null)
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "The agent execution metadata is required."));
        AgentExecutionContext executionContext = new(request.ExecutionId, request.Metadata);
        if (!credentialService.Validate(credential ?? string.Empty, executionContext))
            return Results.Unauthorized();

        try
        {
            AgentToolResult result = await runtimeEndpoint.ExecuteAsync(
                new(new AgentToolCall(request.ToolCallId, request.ToolName, request.ArgumentsJson), executionContext), ct);
            return Results.Ok(new ExecutePatientAgentToolResponse(result.CallId, result.Output, result.IsError));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return Results.Json(new ClinicError(ClinicErrorCode.Forbidden, "The requested agent tool is not permitted."), statusCode: StatusCodes.Status403Forbidden);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "The requested patient-agent tool is not available."));
        }
        catch (Exception exception) when (exception is System.Text.Json.JsonException or FormatException or ArgumentException or InvalidOperationException)
        {
            return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "The agent tool arguments are invalid."));
        }
    }

    private static async Task<IResult> GetPatientAgentActionAsync(
        Guid patientId, Guid actionId, [FromServices] IPatientAgentContextFactory contextFactory,
        ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty) return InvalidActionRequest();
        PatientAgentContext context = await contextFactory.CreateAsync(patientId, null, ct);
        PatientAgentActionResult result = await sender.Send(new GetPatientAgentAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(ToDetails(action)));
    }

    private static async Task<IResult> GetOperationsAgentActionAsync(Guid actionId, [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty) return InvalidActionRequest();
        PatientAgentContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAgentAction(); }
        PatientAgentActionResult result = await sender.Send(new GetPatientAgentAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(ToDetails(action)));
    }

    private static async Task<IResult> ConfirmOperationsAgentActionAsync(Guid actionId, [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty) return InvalidActionRequest();
        PatientAgentContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAgentAction(); }
        PatientAgentActionResult result = await sender.Send(new ConfirmPatientAgentAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(new ConfirmPatientAgentActionResponse(action.ActionId, ToContract(action.Status), Json(action.CompletionPayload), ToFailure(action.Failure))));
    }

    private static async Task<IResult> RejectOperationsAgentActionAsync(Guid actionId, RejectPatientAgentActionRequest request, [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (actionId == Guid.Empty || request is null || !Guid.TryParse(request.ActionId, out Guid bodyId) || bodyId != actionId || request.Reason?.Length > 1_000) return InvalidActionRequest();
        PatientAgentContext context;
        try { context = await contextFactory.CreateOperationsAsync(null, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAgentAction(); }
        PatientAgentActionResult result = await sender.Send(new RejectPatientAgentAction(actionId, context, request.Reason), ct);
        return MapActionResult(result, action => Results.Ok(new RejectPatientAgentActionResponse(action.ActionId, ToContract(action.Status))));
    }

    private static async Task<IResult> RunOperationsAgentAsync(RunPatientAgentRequest request, [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message)) return Results.BadRequest(new ClinicError(ClinicErrorCode.ValidationFailure, "A message is required."));
        PatientAgentContext context;
        try { context = await contextFactory.CreateOperationsAsync(request.ConversationId, ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAgentAction(); }
        PatientAgentResult result = await sender.Send(new RunPatientAgent(context, new PatientAgentInput(request.Message)), ct);
        if (result.Outcome == PatientAgentRunOutcome.NotFound) return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."));
        if (result.Outcome == PatientAgentRunOutcome.Busy) return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active."));
        if (result.Outcome != PatientAgentRunOutcome.Succeeded) return Results.Json(new ClinicError(ClinicErrorCode.UnexpectedFailure, "The operations agent failed."), statusCode: StatusCodes.Status502BadGateway);
        return Results.Ok(new RunPatientAgentResponse(result.AgentRunId, result.ConversationId, null, result.Messages.Select(message => new PatientAgentMessageResponse((PatientAgentMessageResponseKind)(int)message.Kind, message.Content)).ToArray(), result.Actions.Select(action => new PatientAgentActionResponse(action.ActionId.ToString("D"), action.Capability, action.ActionType, ToContract(action.Status), Json(action.ReviewPayload)!.Value, action.Summary)).ToArray()));
    }

    private static async Task<IResult> ConfirmPatientAgentActionAsync(
        Guid patientId, Guid actionId, ConfirmPatientAgentActionRequest request,
        [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty || request is null ||
            !Guid.TryParse(request.ActionId, out Guid bodyActionId) || bodyActionId != actionId)
            return InvalidActionRequest();
        PatientAgentContext context = await contextFactory.CreateAsync(patientId, null, ct);
        PatientAgentActionResult result = await sender.Send(new ConfirmPatientAgentAction(actionId, context), ct);
        return MapActionResult(result, action => Results.Ok(new ConfirmPatientAgentActionResponse(
            action.ActionId, ToContract(action.Status), Json(action.CompletionPayload), ToFailure(action.Failure))));
    }

    private static async Task<IResult> RejectPatientAgentActionAsync(
        Guid patientId, Guid actionId, RejectPatientAgentActionRequest request,
        [FromServices] IPatientAgentContextFactory contextFactory, ISender sender, CancellationToken ct)
    {
        if (patientId == Guid.Empty || actionId == Guid.Empty || request is null ||
            !Guid.TryParse(request.ActionId, out Guid bodyActionId) || bodyActionId != actionId ||
            request.Reason?.Length > 1_000)
            return InvalidActionRequest();
        PatientAgentContext context = await contextFactory.CreateAsync(patientId, null, ct);
        PatientAgentActionResult result = await sender.Send(new RejectPatientAgentAction(actionId, context, request.Reason), ct);
        return MapActionResult(result, action => Results.Ok(new RejectPatientAgentActionResponse(
            action.ActionId, ToContract(action.Status))));
    }

    private static IResult MapActionResult(PatientAgentActionResult result, Func<PatientAgentAction, IResult> success) =>
        result.Outcome switch
        {
            PatientAgentActionOutcome.Succeeded when result.Action is not null => success(result.Action),
            PatientAgentActionOutcome.ValidationFailure => InvalidActionRequest(),
            PatientAgentActionOutcome.NotFound => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient-agent action was not found.")),
            PatientAgentActionOutcome.ScopeMismatch => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient-agent action was not found.")),
            PatientAgentActionOutcome.PermissionDenied => Results.Json(new ClinicError(ClinicErrorCode.Forbidden, "The action is not permitted."), statusCode: StatusCodes.Status403Forbidden),
            PatientAgentActionOutcome.Expired => Results.Conflict(ToDetails(result.Action!)),
            PatientAgentActionOutcome.Conflict => Results.Conflict(result.Action is null ? new ClinicError(ClinicErrorCode.Conflict, "The action cannot be changed.") : ToDetails(result.Action)),
            PatientAgentActionOutcome.Failed => Results.Json(ToDetails(result.Action!), statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Results.Json(new ClinicError(ClinicErrorCode.UnexpectedFailure, "The action operation failed."), statusCode: StatusCodes.Status500InternalServerError)
        };

    private static PatientAgentActionDetailsResponse ToDetails(PatientAgentAction action) => new(
        action.ActionId, action.AgentRunId, action.ConversationId, action.Capability, action.ActionType,
        ToContract(action.Status), Json(action.ReviewPayload)!.Value, action.ExpiresAt,
        Json(action.CompletionPayload), ToFailure(action.Failure));

    private static Contracts.PatientAgent.PatientAgentActionStatus ToContract(Application.PatientAgent.PatientAgentActionStatus status) =>
        (Contracts.PatientAgent.PatientAgentActionStatus)(int)status;

    private static PatientAgentActionFailureResponse? ToFailure(PatientAgentActionFailure? failure) =>
        failure is null ? null : new(failure.Code, failure.Message, failure.Retryable);

    private static System.Text.Json.JsonElement? Json(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(value);

    private static IResult InvalidActionRequest() => Results.BadRequest(
        new ClinicError(ClinicErrorCode.ValidationFailure, "A valid action identifier is required."));

    private static IResult ForbiddenAgentAction() => Results.Json(
        new ClinicError(ClinicErrorCode.Forbidden, "The operations agent is not permitted."), statusCode: StatusCodes.Status403Forbidden);

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
        if (result.Outcome == PatientAgentRunOutcome.NotFound)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient-agent conversation was not found."));
        if (result.Outcome == PatientAgentRunOutcome.Busy)
            return Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active."));
        return Results.Ok(new RunPatientAgentResponse(
            result.AgentRunId,
            result.ConversationId,
            null,
            result.Messages.Select(message => new PatientAgentMessageResponse(
                (PatientAgentMessageResponseKind)(int)message.Kind,
                message.Content)).ToArray(),
            result.Actions.Select(action => new PatientAgentActionResponse(
                action.ActionId.ToString("D"),
                action.Capability,
                action.ActionType,
                ToContract(action.Status),
                System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(action.ReviewPayload),
                action.Summary)).ToArray()));
    }

    private static async Task<IResult> DeleteOperationsAgentConversationAsync(
        Guid conversationId,
        [FromServices] IPatientAgentContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
    {
        if (conversationId == Guid.Empty)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."));

        PatientAgentContext context;
        try { context = await contextFactory.CreateOperationsAsync(conversationId.ToString("N"), ct); }
        catch (UnauthorizedAccessException) { return ForbiddenAgentAction(); }

        AgentConversationOutcome outcome = await sender.Send(new DeleteAgentConversation(
            conversationId,
            new AgentConversationScope(
                context.PracticeId,
                context.AuthenticatedUserId,
                "clinic.operations",
                "practice",
                "practice")), ct);
        return outcome switch
        {
            AgentConversationOutcome.Succeeded => Results.NoContent(),
            AgentConversationOutcome.Busy => Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active.")),
            _ => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Operations-agent conversation was not found."))
        };
    }

    private static async Task<IResult> DeletePatientAgentConversationAsync(
        Guid patientId,
        Guid conversationId,
        [FromServices] IPatientAgentContextFactory contextFactory,
        ISender sender,
        CancellationToken ct)
    {
        if (patientId == Guid.Empty || conversationId == Guid.Empty)
            return Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient-agent conversation was not found."));

        PatientAgentContext context = await contextFactory.CreateAsync(patientId, conversationId.ToString("N"), ct);
        AgentConversationOutcome outcome = await sender.Send(new DeleteAgentConversation(
            conversationId,
            new AgentConversationScope(
                context.PracticeId,
                context.AuthenticatedUserId,
                "clinic.patient",
                "patient",
                context.PatientId.ToString("D"))), ct);
        return outcome switch
        {
            AgentConversationOutcome.Succeeded => Results.NoContent(),
            AgentConversationOutcome.Busy => Results.Conflict(new ClinicError(ClinicErrorCode.Conflict, "The conversation is active.")),
            _ => Results.NotFound(new ClinicError(ClinicErrorCode.NotFound, "Patient-agent conversation was not found."))
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
