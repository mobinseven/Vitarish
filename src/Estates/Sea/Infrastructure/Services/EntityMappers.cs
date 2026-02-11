namespace Vitarish.Sea.Infrastructure.Services;

/// <summary>
/// Provides mapping functions for Sea estate entities and DTOs.
/// </summary>
internal static class EntityMappers
{
    // Role mappings
    public static RoleReadDto ToDto(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name
    };

    public static Role ToDomain(RoleWriteDto dto) => new(dto.Name)
    {
        Id = dto.Id
    };

    public static RoleDescriptor ToDomain(RoleDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy
    };

    // Reminder mappings
    public static ReminderReadDto ToDto(Reminder reminder) => new()
    {
        Id = reminder.Id,
        EventId = reminder.EventId,
        Kind = reminder.Kind,
        Recipient = reminder.Recipient,
        RecipientUserId = reminder.RecipientUserId,
        Subject = reminder.Subject,
        TextBody = reminder.TextBody,
        RemindAt = reminder.RemindAt,
        SentAt = reminder.SentAt,
        IsSent = reminder.IsSent
    };

    public static Reminder ToDomain(ReminderWriteDto dto) => new(
        dto.EventId,
        dto.Kind,
        dto.RecipientUserId,
        dto.Recipient,
        dto.Subject,
        dto.TextBody,
        dto.RemindAt)
    {
        Id = dto.Id
    };

    public static ReminderDescriptor ToDomain(ReminderDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        EventId = dto.EventId,
        Kind = dto.Kind,
        RecipientUserId = dto.RecipientUserId,
        RemindFrom = dto.RemindFrom,
        RemindTo = dto.RemindTo,
        SentFrom = dto.SentFrom,
        SentTo = dto.SentTo,
        IsSent = dto.IsSent
    };

    // UserRole mappings
    public static UserRoleReadDto ToDto(UserRole userRole) => new()
    {
        Id = userRole.Id,
        RoleId = userRole.RoleId,
        Type = userRole.Type
    };

    public static UserRole ToDomain(UserRoleWriteDto dto) => new(Guid.Empty, dto.RoleId, dto.Type)
    {
        Id = dto.Id
    };

    public static UserRoleDescriptor ToDomain(UserRoleDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        UserId = dto.UserId,
        RoleId = dto.RoleId,
        Type = dto.Type
    };

    // EventRole mappings
    public static EventRoleReadDto ToDto(EventRole eventRole) => new()
    {
        Id = eventRole.Id,
        EventId = eventRole.EventId,
        RoleId = eventRole.RoleId,
        Type = eventRole.Type
    };

    public static EventRole ToDomain(EventRoleWriteDto dto) => new(dto.EventId, dto.RoleId, dto.Type)
    {
        Id = dto.Id
    };

    public static EventRoleDescriptor ToDomain(EventRoleDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        EventId = dto.EventId,
        RoleId = dto.RoleId,
        Type = dto.Type
    };

    // Project mappings
    public static ProjectReadDto ToDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        Template = project.Template,
        Metadata = project.Metadata
    };

    public static Project ToDomain(ProjectWriteDto dto) => new(dto.Name, dto.Description, dto.Template, dto.Metadata)
    {
        Id = dto.Id
    };

    public static ProjectDescriptor ToDomain(ProjectDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        Template = dto.Template,
        SortBy = dto.SortBy,
        Descending = dto.Descending,
        RoleIds = dto.RoleIds
    };

    // Procedure mappings
    public static ProcedureReadDto ToDto(Procedure procedure) => new()
    {
        Id = procedure.Id,
        ProjectId = procedure.ProjectId,
        Name = procedure.Name,
        Metadata = procedure.Metadata
    };

    public static Procedure ToDomain(ProcedureWriteDto dto) => new(dto.ProjectId, dto.Name, dto.Metadata)
    {
        Id = dto.Id
    };

    public static ProcedureDescriptor ToDomain(ProcedureDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        ProjectId = dto.ProjectId,
        RoleIds = dto.RoleIds
    };

    // Event mappings
    public static EventReadDto ToDto(Event @event) => new()
    {
        Id = @event.Id,
        ProjectId = @event.ProjectId,
        ProcedureId = @event.ProcedureId,
        Title = @event.Title,
        Description = @event.Description,
        PreviousEventId = @event.PreviousEventId,
        CommittedBy = @event.CommittedBy,
        CommittedAt = @event.CommittedAt
    };

    public static Event ToDomain(EventWriteDto dto) => new(dto.ProjectId, dto.ProcedureId, dto.PreviousEventId)
    {
        Id = dto.Id,
        Title = dto.Title,
        Description = dto.Description
    };

    public static EventDescriptor ToDomain(EventDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        ProjectId = dto.ProjectId,
        ProcedureId = dto.ProcedureId,
        PreviousEventId = dto.PreviousEventId,
        CommittedBy = dto.CommittedBy,
        CommittedFrom = dto.CommittedFrom,
        CommittedTo = dto.CommittedTo,
        IsCommitted = dto.IsCommitted,
        ReadRoleId = dto.ReadRoleId,
        WriteRoleId = dto.WriteRoleId
    };

    // Attachment mappings
    public static AttachmentReadDto ToDto(Attachment attachment) => new()
    {
        Id = attachment.Id,
        Content = attachment.Content,
        ContentType = attachment.ContentType,
        ContentThumbnail = attachment.ContentThumbnail,
        Size = attachment.Size
    };

    // Note: EventId not in WriteDto - it's set by service when calling AddAsync(eventId, attachments)
    public static Attachment ToDomain(AttachmentWriteDto dto) => new(
        Guid.Empty,  // EventId set by service
        dto.Content,
        dto.ContentType,
        dto.ContentThumbnail,
        dto.Size)
    {
        Id = dto.Id
    };

    public static AttachmentDescriptor ToDomain(AttachmentDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        EventId = dto.EventId,
        ContentType = dto.ContentType,
        ContentThumbnail = dto.ContentThumbnail
    };

    // Receipt mappings
    public static ReceiptReadDto ToDto(Receipt receipt) => new()
    {
        Id = receipt.Id,
        ObjectId = receipt.ObjectId
    };

    public static Receipt ToDomain(ReceiptWriteDto dto) => new(dto.ObjectId)
    {
        Id = dto.Id
    };

    public static ReceiptDescriptor ToDomain(ReceiptDescriptorDto dto) => new()
    {
        Id = dto.Id,
        Search = dto.Search,
        Skip = dto.Skip,
        Take = dto.Take,
        CreatedFrom = dto.CreatedFrom,
        CreatedTo = dto.CreatedTo,
        CreatedBy = dto.CreatedBy,
        UpdatedFrom = dto.UpdatedFrom,
        UpdatedTo = dto.UpdatedTo,
        UpdatedBy = dto.UpdatedBy,
        DeletedFrom = dto.DeletedFrom,
        DeletedTo = dto.DeletedTo,
        DeletedBy = dto.DeletedBy,
        ProjectId = dto.ProjectId,
        ProcedureId = dto.ProcedureId,
        EventId = dto.EventId,
        ObjectId = dto.ObjectId
    };

    // Stats mappings (domain model to DTO)
    public static StatsReadDto ToDto(Stats stats) => new()
    {
        State = stats.State,
        Procedures = stats.Procedures,
        Events = stats.Events,
        CommittedEvents = stats.CommittedEvents,
        EventItems = stats.EventItems,
        NewItems = stats.NewItems,
        Unread = stats.Unread,
        LastUpdate = stats.LastUpdate
    };

    // DatabaseStatistics mappings (domain model to DTO)
    public static DatabaseStatisticsReadDto ToDto(DatabaseStatistics stats) => new()
    {
        Projects = stats.Projects,
        Procedures = stats.Procedures,
        Events = stats.Events,
        Roles = stats.Roles,
        Attachments = stats.Attachments,
        Reminders = stats.Reminders
    };
}
