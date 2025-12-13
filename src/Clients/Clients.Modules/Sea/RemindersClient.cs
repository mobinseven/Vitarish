using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IRemindersClient
    {
        Task<IReadOnlyList<ReminderDto>> AddRangeAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
        Task<ReminderDto?> UpdateAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
        Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class RemindersClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<RemindersClientDefinition>(connectionFactory),
            IRemindersClient
    {
        public async Task<IReadOnlyList<ReminderDto>> AddRangeAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<ReminderDto>>(nameof(AddRangeAsync), cancellationToken, new object?[] { eventId, recipients, subject, textBody, remindAt }).ConfigureAwait(false);

        public async Task<ReminderDto?> UpdateAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default) =>
            await InvokeAsync<ReminderDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { id, kind, recipientUserId, recipient, subject, textBody, remindAt }).ConfigureAwait(false);

        public async Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<ReminderDto>>(nameof(GetByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<ReminderDto>>(nameof(GetDueAsync), cancellationToken, new object?[] { now }).ConfigureAwait(false);

        public async Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkSentAsync), cancellationToken, new object?[] { id, sentAt }).ConfigureAwait(false);
    }
}
