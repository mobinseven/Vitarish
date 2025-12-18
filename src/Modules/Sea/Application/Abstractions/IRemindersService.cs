using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Application.Abstractions;

public interface IRemindersService : IEntityService<ReminderDto, ReminderWriteDto, ReminderDescriptorDto>
{
    Task<IReadOnlyList<ReminderDto>> AddRangeAsync(
        Guid eventId,
        IReadOnlyList<ReminderRecipientDto> recipients,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken);

    Task<ReminderDto?> UpdateAsync(
        Guid id,
        ReminderKind kind,
        string recipientUserId,
        string recipient,
        string subject,
        string textBody,
        DateTimeOffset remindAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken);
}

