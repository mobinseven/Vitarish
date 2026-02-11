using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Application.Abstractions;

public interface IRemindersService : IEntityService<ReminderReadDto, ReminderWriteDto, ReminderDescriptorDto>
{
    Task<IReadOnlyList<ReminderReadDto>> AddRangeAsync(
        Guid eventId,
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

    Task<IReadOnlyList<ReminderReadDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderReadDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken);
}

