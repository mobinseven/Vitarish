using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProjectsHub : EntityServer<ProjectDto, ProjectDescriptorDto, IProjectsService>
{
    private readonly IProjectsService _service;

    public ProjectsHub(IProjectsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> AddAsync(string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        _service.AddAsync(name, description, template, metadata, cancellationToken);

    public Task UpdateAsync(Guid id, string name, string description, bool template, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, description, template, metadata, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProjectSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}

public sealed class ProceduresHub : EntityServer<ProcedureDto, ProcedureDescriptorDto, IProceduresService>
{
    private readonly IProceduresService _service;

    public ProceduresHub(IProceduresService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> AddAsync(Guid projectId, string name, CancellationToken cancellationToken) =>
        _service.AddAsync(projectId, name, cancellationToken);

    public Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        _service.DeleteAsync(id, cancellationToken);

    public Task UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken) =>
        _service.UpdateMetadataAsync(id, metadata, cancellationToken);

    public Task<ProcedureSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}

public sealed class EventsHub : EntityServer<EventDto, EventDescriptorDto, IEventsService>
{
    private readonly IEventsService _service;

    public EventsHub(IEventsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<IReadOnlyList<EventDto>> GetByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.GetByProcedureAsync(procedureId, cancellationToken);

    public Task<EventDto?> AddAsync(Guid projectId, Guid procedureId, Guid? previousEventId, CancellationToken cancellationToken) =>
        _service.AddAsync(projectId, procedureId, previousEventId, cancellationToken);

    public Task<EventDto?> UpdateAsync(Guid id, string title, string description, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, title, description, cancellationToken);

    public Task<bool> MoveUpAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MoveUpAsync(procedureId, eventId, cancellationToken);

    public Task<bool> MoveDownAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MoveDownAsync(procedureId, eventId, cancellationToken);

    public Task<bool> RemoveEventAsync(Guid procedureId, Guid eventId, CancellationToken cancellationToken) =>
        _service.RemoveEventAsync(procedureId, eventId, cancellationToken);

    public Task RepairChainAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.RepairChainAsync(procedureId, cancellationToken);

    public Task UpdateChainAsync(Guid procedureId, IEnumerable<EventChainChangeDto> chainChangeDtos, CancellationToken cancellationToken) =>
        _service.UpdateChainAsync(procedureId, chainChangeDtos, cancellationToken);

    public Task CommitAsync(Guid eventId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        _service.CommitAsync(eventId, reporterUserId, commitTime, cancellationToken);

    public Task<int> CommitAllByProcedureAsync(Guid procedureId, Guid reporterUserId, DateTimeOffset commitTime, CancellationToken cancellationToken) =>
        _service.CommitAllByProcedureAsync(procedureId, reporterUserId, commitTime, cancellationToken);

    public Task UncommitAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.UncommitAsync(eventId, cancellationToken);

    public Task<int> UncommitAllByProcedureAsync(Guid procedureId, CancellationToken cancellationToken) =>
        _service.UncommitAllByProcedureAsync(procedureId, cancellationToken);

    public Task<int> GetRemindersCountAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetRemindersCountAsync(eventId, cancellationToken);

    public Task<EventSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}

public sealed class AttachmentsHub : EntityServer<AttachmentDto, AttachmentDescriptorDto, IAttachmentsService>
{
    private readonly IAttachmentsService _service;

    public AttachmentsHub(IAttachmentsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetByEventAsync(eventId, cancellationToken);

    public Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetCountByEventAsync(eventId, cancellationToken);

    public Task<AttachmentDto?> AddAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken) =>
        _service.AddAsync(eventId, content, contentType, contentThumbnail, size, cancellationToken);

    public Task AddAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken) =>
        _service.AddAsync(eventId, attachments, cancellationToken);

    public Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken) =>
        _service.RemoveAsync(eventId, attachmentId, cancellationToken);

    public Task<AttachmentDto?> UpdateAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken) =>
        _service.UpdateAsync(eventId, attachmentId, content, cancellationToken);

    public Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}

public sealed class EventRolesHub : EntityServer<EventRoleDto, EventRoleDescriptorDto, IEventRolesService>
{
    private readonly IEventRolesService _service;

    public EventRolesHub(IEventRolesService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<(IReadOnlyList<Guid> ReadRoleIds, IReadOnlyList<Guid> WriteRoleIds)> GetIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetReadIdsAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetWriteIdsAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetWriteIdsAsync(eventId, cancellationToken);

    public Task AssignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        _service.AssignAsync(eventId, roleId, isWrite, cancellationToken);

    public Task UnassignAsync(Guid eventId, Guid roleId, bool isWrite, CancellationToken cancellationToken) =>
        _service.UnassignAsync(eventId, roleId, isWrite, cancellationToken);
}

public sealed class RolesHub : EntityServer<RoleDto, RoleDescriptorDto, IRolesService>
{
    private readonly IRolesService _service;

    public RolesHub(IRolesService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> AddAsync(string name, CancellationToken cancellationToken) =>
        _service.AddAsync(name, cancellationToken);

    public Task UpdateAsync(Guid id, string name, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, name, cancellationToken);

    public Task<(IReadOnlyList<RoleDto> Items, int TotalItems)> GetPagedAsync(RoleDescriptorDto descriptor, CancellationToken cancellationToken) =>
        _service.GetPagedAsync(descriptor, cancellationToken);
}

public sealed class UserRolesHub : EntityServer<UserRoleDto, UserRoleDescriptorDto, IUserRolesService>
{
    private readonly IUserRolesService _service;

    public UserRolesHub(IUserRolesService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task AssignAsync(Guid userId, Guid roleId, RoleType type, CancellationToken cancellationToken) =>
        _service.AssignAsync(userId, roleId, type, cancellationToken);

    public Task RemoveAsync(Guid userId, Guid roleId, CancellationToken cancellationToken) =>
        _service.RemoveAsync(userId, roleId, cancellationToken);
}

public sealed class ReceiptsHub : EntityServer<ReceiptDto, ReceiptDescriptorDto, IReceiptsService>
{
    private readonly IReceiptsService _service;

    public ReceiptsHub(IReceiptsService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        _service.AddAsync(userId, objectId, cancellationToken);

    public Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken) =>
        _service.GetReadersAsync(objectId, cancellationToken);

    public Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        _service.MarkProjectReadAsync(userId, projectId, cancellationToken);

    public Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        _service.MarkProjectUnreadAsync(userId, projectId, cancellationToken);

    public Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        _service.MarkProcedureReadAsync(userId, procedureId, cancellationToken);

    public Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken) =>
        _service.MarkProcedureUnreadAsync(userId, procedureId, cancellationToken);

    public Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MarkEventReadAsync(userId, eventId, cancellationToken);

    public Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken) =>
        _service.MarkEventUnreadAsync(userId, eventId, cancellationToken);

    public Task<bool> GetAsync(Guid userId, Guid objectId, CancellationToken cancellationToken) =>
        _service.GetAsync(userId, objectId, cancellationToken);
}

public sealed class RemindersHub : EntityServer<ReminderDto, ReminderDescriptorDto, IRemindersService>
{
    private readonly IRemindersService _service;

    public RemindersHub(IRemindersService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<IReadOnlyList<ReminderDto>> AddRangeAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken) =>
        _service.AddRangeAsync(eventId, recipients, subject, textBody, remindAt, cancellationToken);

    public Task<ReminderDto?> UpdateAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, kind, recipientUserId, recipient, subject, textBody, remindAt, cancellationToken);

    public Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetByEventAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        _service.GetDueAsync(now, cancellationToken);

    public Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken) =>
        _service.MarkSentAsync(id, sentAt, cancellationToken);
}

public sealed class UsageConfigurationHub : EntityServer<UsageConfigurationDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>
{
    private readonly IUsageConfigurationService _service;

    public UsageConfigurationHub(IUsageConfigurationService service, IEntityUserService entityUserService)
        : base(service, entityUserService)
    {
        _service = service;
    }

    public Task<UsageConfigurationDto?> GetAsync(CancellationToken cancellationToken) =>
        _service.GetAsync(cancellationToken);

    public Task<UsageConfigurationDto?> UpdateAsync(UsageConfigurationDto usageConfigurationDto, CancellationToken cancellationToken) =>
        _service.UpdateAsync(usageConfigurationDto, cancellationToken);
}
