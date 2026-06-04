using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Communications.Application.Abstractions;

public interface IRemindersService : IEntityService<ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>
{
    Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid sourceId,
        IReadOnlyList<ReminderRecipientReadDto> recipients,
        string subject,
        string textBody,
        DateTime remindAt,
        CancellationToken cancellationToken);

    Task<ReminderReadDto?> UpdateAsync(
        Guid id,
        ReminderKind kind,
        string recipientUserId,
        string recipient,
        string subject,
        string textBody,
        DateTime remindAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderReadDto>> GetBySourceAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTime now, CancellationToken cancellationToken);

    Task MarkSentAsync(Guid id, DateTime sentAt, CancellationToken cancellationToken);
}
