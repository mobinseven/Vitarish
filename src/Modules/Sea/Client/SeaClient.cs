using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Vitarish.Modules.Aggregator.Client;
using Vitarish.Sea.Contracts.Errors;
using Vitarish.Sea.Contracts.Responses;
using Vitarish.Sea.Contracts.Results;
using Vitarish.Sea.Contracts.Routes;

namespace Vitarish.Sea.Client;

public sealed class SeaClient(HttpClient httpClient) : AuthenticatedModuleApiClient(httpClient), ISeaClient
{
    protected override ModuleApiClientFailure CreateFailure(HttpStatusCode statusCode, string message, int? statusCodeValue) => new(statusCode.ToString(), message, statusCodeValue);

    private static SeaError Error(SeaErrorCode code, string message) => new(code, message);
    private static Task<SeaResult<T>> Failure<T>(SeaErrorCode code, string message) => Task.FromResult(SeaResult<T>.Failure(Error(code, message)));
    private static Task<SeaResult<SeaCommandResult>> CommandFailure(SeaErrorCode code, string message) => Task.FromResult(SeaResult<SeaCommandResult>.Failure(Error(code, message)));

    private async Task<SeaResult<T>> SendAsync<T>(HttpMethod method, string route, object? body, CancellationToken cancellationToken)
    {
        try
        {
            using HttpRequestMessage request = new(method, SeaApiRoutes.Base + route);
            if (body is not null) request.Content = JsonContent.Create(body);
            ModuleApiClientResult<HttpResponseMessage> sent = await SendRawAsync(request, cancellationToken).ConfigureAwait(false);
            if (!sent.IsSuccess) return SeaResult<T>.Failure(MapTransportFailure(sent.Failure));
            using HttpResponseMessage response = sent.Value!;
            if (!response.IsSuccessStatusCode) return SeaResult<T>.Failure(await MapErrorAsync(response, cancellationToken).ConfigureAwait(false));
            T? value = await response.Content.ReadFromJsonAsync<T>(cancellationToken).ConfigureAwait(false);
            return value is null ? SeaResult<T>.Failure(Error(SeaErrorCode.DeserializationFailure, "The Sea API returned an empty response.")) : SeaResult<T>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return SeaResult<T>.Failure(Error(SeaErrorCode.Cancellation, "The Sea request was cancelled by the caller.")); }
        catch (TaskCanceledException ex) { return SeaResult<T>.Failure(Error(SeaErrorCode.Timeout, ex.Message)); }
        catch (JsonException ex) { return SeaResult<T>.Failure(Error(SeaErrorCode.DeserializationFailure, ex.Message)); }
        catch (HttpRequestException ex) { return SeaResult<T>.Failure(Error(SeaErrorCode.TransportFailure, ex.Message)); }
        catch (Exception ex) { return SeaResult<T>.Failure(Error(SeaErrorCode.UnexpectedFailure, ex.Message)); }
    }

    private static SeaError MapTransportFailure(ModuleApiClientFailure? failure)
    {
        if (failure is null) return Error(SeaErrorCode.TransportFailure, "Sea API request failed.");
        return failure.Code switch
        {
            "Canceled" => Error(SeaErrorCode.Cancellation, failure.Message),
            "Timeout" => Error(SeaErrorCode.Timeout, failure.Message),
            _ => Error(SeaErrorCode.TransportFailure, failure.Message)
        };
    }

    private static async Task<SeaError> MapErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        SeaErrorCode code = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => SeaErrorCode.ValidationFailure,
            HttpStatusCode.Unauthorized => SeaErrorCode.Unauthorized,
            HttpStatusCode.Forbidden => SeaErrorCode.Forbidden,
            HttpStatusCode.NotFound => SeaErrorCode.NotFound,
            HttpStatusCode.Conflict => SeaErrorCode.Conflict,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => SeaErrorCode.Timeout,
            _ => SeaErrorCode.UnexpectedFailure
        };
        if (response.Content is null)
        {
            return Error(code, $"Sea API returned {(int)response.StatusCode} {response.ReasonPhrase} with no content.");
        }

        try
        {
            SeaError? structured = await response.Content.ReadFromJsonAsync<SeaError>(cancellationToken).ConfigureAwait(false);
            return structured ?? Error(code, $"Sea API returned {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        catch (JsonException ex)
        {
            return Error(code, $"Sea API returned {(int)response.StatusCode} {response.ReasonPhrase}. Deserialization failed: {ex.Message}");
        }
    }

    public Task<SeaResult<IReadOnlyList<ProjectReadDto>>> ListProjectsAsync(CancellationToken cancellationToken) => SendAsync<IReadOnlyList<ProjectReadDto>>(HttpMethod.Get, SeaApiRoutes.Projects, null, cancellationToken);
    public Task<SeaResult<ProjectReadDto>> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<ProjectReadDto>(HttpMethod.Get, $"/projects/{projectId}", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CreateProjectAsync(ProjectWriteDto project, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Projects, project, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UpdateProjectAsync(ProjectWriteDto project, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Put, SeaApiRoutes.Projects, project, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/projects/{projectId}", null, cancellationToken);
    public Task<SeaResult<ProjectSnapshot>> GetProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<ProjectSnapshot>(HttpMethod.Get, $"/projects/{projectId}/snapshot", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkProjectReadAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/projects/{projectId}/read", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkProjectUnreadAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/projects/{projectId}/unread", null, cancellationToken);

    public Task<SeaResult<IReadOnlyList<ProcedureReadDto>>> ListProceduresAsync(Guid projectId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<ProcedureReadDto>>(HttpMethod.Get, $"/projects/{projectId}/procedures", null, cancellationToken);
    public Task<SeaResult<ProcedureReadDto>> GetProcedureAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<ProcedureReadDto>(HttpMethod.Get, $"/procedures/{procedureId}", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CreateProcedureAsync(ProcedureWriteDto procedure, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Procedures, procedure, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UpdateProcedureAsync(ProcedureWriteDto procedure, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Put, SeaApiRoutes.Procedures, procedure, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> DeleteProcedureAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/procedures/{procedureId}", null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<ProcedureReadDto>>> ListProcedureTemplatesAsync(CancellationToken cancellationToken) => SendAsync<IReadOnlyList<ProcedureReadDto>>(HttpMethod.Get, "/procedures/templates", null, cancellationToken);
    public Task<SeaResult<ProcedureSnapshot>> GetProcedureSnapshotAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<ProcedureSnapshot>(HttpMethod.Get, $"/procedures/{procedureId}/snapshot", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkProcedureReadAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/procedures/{procedureId}/read", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkProcedureUnreadAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/procedures/{procedureId}/unread", null, cancellationToken);

    public Task<SeaResult<IReadOnlyList<EventReadDto>>> ListEventsByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<EventReadDto>>(HttpMethod.Get, $"/procedures/{procedureId}/events", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CreateEventAfterAsync(Guid? previousEventId, EventWriteDto @event, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/after/{previousEventId}", @event, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UpdateEventAsync(EventWriteDto @event, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Put, SeaApiRoutes.Events, @event, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MoveEventUpAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/move-up", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MoveEventDownAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/move-down", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> RemoveEventAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/events/{eventId}", null, cancellationToken);
    public Task<SeaResult<EventsChainChangeReadDto>> RepairEventChainAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<EventsChainChangeReadDto>(HttpMethod.Post, $"/procedures/{procedureId}/events/repair", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CommitEventAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/commit", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UncommitEventAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/uncommit", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CommitAllEventsAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/procedures/{procedureId}/events/commit", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UncommitAllEventsAsync(Guid procedureId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/procedures/{procedureId}/events/uncommit", null, cancellationToken);
    public Task<SeaResult<int>> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<int>(HttpMethod.Get, $"/events/{eventId}/reminders/count", null, cancellationToken);
    public Task<SeaResult<EventSnapshot>> GetEventSnapshotAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<EventSnapshot>(HttpMethod.Get, $"/events/{eventId}/snapshot", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkEventReadAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/read", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> MarkEventUnreadAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, $"/events/{eventId}/unread", null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<AttachmentReadDto>>> ListAttachmentsByEventAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<AttachmentReadDto>>(HttpMethod.Get, $"/events/{eventId}/attachments", null, cancellationToken);
    public Task<SeaResult<int>> CountAttachmentsByEventAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<int>(HttpMethod.Get, $"/events/{eventId}/attachments/count", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AddTextAttachmentMetadataAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Attachments + "/text", attachment, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AddFileAttachmentMetadataAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Attachments + "/file", attachment, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AddAttachmentsAsync(IReadOnlyList<AttachmentWriteDto> attachments, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Attachments + "/batch", attachments, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UpdateTextAttachmentAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Put, SeaApiRoutes.Attachments + "/text", attachment, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> RemoveAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/attachments/{attachmentId}", null, cancellationToken);
    public Task<SeaResult<AttachmentSnapshot>> GetAttachmentSnapshotAsync(Guid attachmentId, CancellationToken cancellationToken) => SendAsync<AttachmentSnapshot>(HttpMethod.Get, $"/attachments/{attachmentId}/snapshot", null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<RoleReadDto>>> ListRolesAsync(CancellationToken cancellationToken) => SendAsync<IReadOnlyList<RoleReadDto>>(HttpMethod.Get, SeaApiRoutes.Roles, null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<RoleReadDto>>> PageRolesAsync(int page, int pageSize, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<RoleReadDto>>(HttpMethod.Get, $"/roles?page={page}&pageSize={pageSize}", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> CreateRoleAsync(RoleWriteDto role, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Roles, role, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UpdateRoleAsync(RoleWriteDto role, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Put, SeaApiRoutes.Roles, role, cancellationToken);
    public Task<SeaResult<IReadOnlyList<UserRoleReadDto>>> ListUserRolesAsync(string userId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<UserRoleReadDto>>(HttpMethod.Get, $"/users/{Uri.EscapeDataString(userId)}/roles", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AssignUserRoleAsync(UserRoleWriteDto userRole, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.UserRoles, userRole, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> RemoveUserRoleAsync(Guid userRoleId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/user-roles/{userRoleId}", null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<Guid>>> GetEventRoleIdsAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<Guid>>(HttpMethod.Get, $"/events/{eventId}/roles", null, cancellationToken);
    public Task<SeaResult<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>> GetEventRoleIdsForEventsAsync(IReadOnlyList<Guid> eventIds, CancellationToken cancellationToken) => SendAsync<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>(HttpMethod.Post, "/events/roles", eventIds, cancellationToken);
    public Task<SeaResult<IReadOnlyList<Guid>>> GetEventReadRoleIdsAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<Guid>>(HttpMethod.Get, $"/events/{eventId}/roles/read", null, cancellationToken);
    public Task<SeaResult<IReadOnlyList<Guid>>> GetEventWriteRoleIdsAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<Guid>>(HttpMethod.Get, $"/events/{eventId}/roles/write", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AssignEventRoleAsync(EventRoleWriteDto eventRole, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.EventRoles, eventRole, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> UnassignEventRoleAsync(Guid eventRoleId, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Delete, $"/event-roles/{eventRoleId}", null, cancellationToken);
    public Task<SeaResult<SeaCommandResult>> AddReceiptAsync(ReceiptWriteDto receipt, CancellationToken cancellationToken) => SendAsync<SeaCommandResult>(HttpMethod.Post, SeaApiRoutes.Receipts, receipt, cancellationToken);
    public Task<SeaResult<IReadOnlyList<ReceiptReadDto>>> ListReadersAsync(Guid eventId, CancellationToken cancellationToken) => SendAsync<IReadOnlyList<ReceiptReadDto>>(HttpMethod.Get, $"/events/{eventId}/readers", null, cancellationToken);
    public Task<SeaResult<bool>> HasReceiptAsync(Guid eventId, string userId, CancellationToken cancellationToken) => SendAsync<bool>(HttpMethod.Get, $"/events/{eventId}/receipts/{Uri.EscapeDataString(userId)}", null, cancellationToken);
    public Task<SeaResult<IReadOnlyDictionary<Guid, bool>>> HasReceiptsAsync(IReadOnlyList<Guid> eventIds, string userId, CancellationToken cancellationToken) => SendAsync<IReadOnlyDictionary<Guid, bool>>(HttpMethod.Post, $"/receipts/{Uri.EscapeDataString(userId)}/events", eventIds, cancellationToken);
    public Task<SeaResult<DatabaseStatisticsReadDto>> GetDatabaseStatisticsAsync(CancellationToken cancellationToken) => SendAsync<DatabaseStatisticsReadDto>(HttpMethod.Get, SeaApiRoutes.Statistics, null, cancellationToken);
}
