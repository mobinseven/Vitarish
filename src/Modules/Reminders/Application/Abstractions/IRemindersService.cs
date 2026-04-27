using Vitarish.Entities.Server.Abstractions;
using Vitarish.Reminders.Domain.Dto;
using Vitarish.Reminders.Domain.Enums;

namespace Vitarish.Reminders.Application.Abstractions;

public interface IRemindersService : IEntityService<ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>
{
    Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid sourceId,
        IReadOnlyList<ReminderRecipientReadDto> recipients,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken);

    Task<ReminderReadDto?> UpdateAsync(
        Guid id,
        ReminderKind kind,
        string recipientUserId,
        string recipient,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderReadDto>> GetBySourceAsync(Guid sourceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken);
}
