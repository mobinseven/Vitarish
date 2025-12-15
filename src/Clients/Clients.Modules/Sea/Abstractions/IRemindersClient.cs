using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IRemindersClient : IEntityClient<ReminderDto, ReminderDescriptorDto>
{
    Task<IReadOnlyList<ReminderDto>> AddRangeAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
    Task<ReminderDto?> UpdateAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
}
