using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IRemindersClient
{
    Task<IReadOnlyList<ReminderDto>> AddRangeAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
    Task<ReminderDto?> UpdateAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
}
