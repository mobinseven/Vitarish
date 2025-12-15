using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting.Hubs;

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

    public Task<ReminderDto?> UpdateReminderAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken) =>
        _service.UpdateAsync(id, kind, recipientUserId, recipient, subject, textBody, remindAt, cancellationToken);

    public Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetByEventAsync(eventId, cancellationToken);

    public Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        _service.GetDueAsync(now, cancellationToken);

    public Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken) =>
        _service.MarkSentAsync(id, sentAt, cancellationToken);
}
