using Vitarish.Sea.Contracts.Responses;
using Vitarish.Sea.Contracts.Results;

namespace Vitarish.Sea.Client;

public interface ISeaClient
{
    Task<SeaResult<SeaCapabilitiesReadDto>> GetCapabilitiesAsync(CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<ProjectReadDto>>> ListProjectsAsync(CancellationToken cancellationToken);
    Task<SeaResult<ProjectReadDto>> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CreateProjectAsync(ProjectWriteDto project, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UpdateProjectAsync(ProjectWriteDto project, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken);
    Task<SeaResult<ProjectSnapshot>> GetProjectSnapshotAsync(Guid projectId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkProjectReadAsync(Guid projectId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkProjectUnreadAsync(Guid projectId, CancellationToken cancellationToken);

    Task<SeaResult<IReadOnlyList<ProcedureReadDto>>> ListProceduresAsync(Guid projectId, CancellationToken cancellationToken);
    Task<SeaResult<ProcedureReadDto>> GetProcedureAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CreateProcedureAsync(ProcedureWriteDto procedure, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UpdateProcedureAsync(ProcedureWriteDto procedure, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> DeleteProcedureAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<ProcedureReadDto>>> ListProcedureTemplatesAsync(CancellationToken cancellationToken);
    Task<SeaResult<ProcedureSnapshot>> GetProcedureSnapshotAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkProcedureReadAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkProcedureUnreadAsync(Guid procedureId, CancellationToken cancellationToken);

    Task<SeaResult<IReadOnlyList<EventReadDto>>> ListEventsByProcedureAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<EventReadDto>> GetEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<EventCapabilitiesReadDto>> GetEventCapabilitiesAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<bool>> CanWriteEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CreateEventAfterAsync(Guid? previousEventId, EventWriteDto @event, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UpdateEventAsync(EventWriteDto @event, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MoveEventUpAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MoveEventDownAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> RemoveEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<EventsChainChangeReadDto>> RepairEventChainAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CommitEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UncommitEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CommitAllEventsAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UncommitAllEventsAsync(Guid procedureId, CancellationToken cancellationToken);
    Task<SeaResult<int>> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<EventSnapshot>> GetEventSnapshotAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkEventReadAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> MarkEventUnreadAsync(Guid eventId, CancellationToken cancellationToken);

    Task<SeaResult<IReadOnlyList<AttachmentReadDto>>> ListAttachmentsByEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<int>> CountAttachmentsByEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AddTextAttachmentMetadataAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AddFileAttachmentMetadataAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AddAttachmentsAsync(IReadOnlyList<AttachmentWriteDto> attachments, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UpdateTextAttachmentAsync(AttachmentWriteDto attachment, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> RemoveAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken);
    Task<SeaResult<AttachmentSnapshot>> GetAttachmentSnapshotAsync(Guid attachmentId, CancellationToken cancellationToken);

    Task<SeaResult<IReadOnlyList<RoleReadDto>>> ListRolesAsync(CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<RoleReadDto>>> PageRolesAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> CreateRoleAsync(RoleWriteDto role, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UpdateRoleAsync(RoleWriteDto role, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<UserRoleReadDto>>> ListUserRolesAsync(string userId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AssignUserRoleAsync(UserRoleWriteDto userRole, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> RemoveUserRoleAsync(Guid userRoleId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<Guid>>> GetEventRoleIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>>> GetEventRoleIdsForEventsAsync(IReadOnlyList<Guid> eventIds, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<Guid>>> GetEventReadRoleIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<Guid>>> GetEventWriteRoleIdsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AssignEventRoleAsync(EventRoleWriteDto eventRole, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> UnassignEventRoleAsync(Guid eventRoleId, CancellationToken cancellationToken);
    Task<SeaResult<SeaCommandResult>> AddReceiptAsync(ReceiptWriteDto receipt, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyList<ReceiptReadDto>>> ListReadersAsync(Guid eventId, CancellationToken cancellationToken);
    Task<SeaResult<bool>> HasReceiptAsync(Guid eventId, string userId, CancellationToken cancellationToken);
    Task<SeaResult<IReadOnlyDictionary<Guid, bool>>> HasReceiptsAsync(IReadOnlyList<Guid> eventIds, string userId, CancellationToken cancellationToken);
    Task<SeaResult<DatabaseStatisticsReadDto>> GetDatabaseStatisticsAsync(CancellationToken cancellationToken);
}
