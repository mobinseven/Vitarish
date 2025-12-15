using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Clients.Modules.Sea.Enums;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class RemindersClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<ReminderDto, ReminderDescriptorDto, RemindersClientDefinition>(connectionFactory, entityInterceptionProvider),
            IRemindersClient
    {
    public async Task<IReadOnlyList<ReminderDto>> AddRemindersAsync(Guid eventId, IReadOnlyList<ReminderRecipientDto> recipients, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default) =>
        await InvokeHubAsync<IReadOnlyList<ReminderDto>>(nameof(AddRemindersAsync), cancellationToken, new object?[] { eventId, recipients, subject, textBody, remindAt }).ConfigureAwait(false);

        public async Task<ReminderDto?> UpdateReminderAsync(Guid id, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<ReminderDto?>(nameof(UpdateReminderAsync), cancellationToken, new object?[] { id, kind, recipientUserId, recipient, subject, textBody, remindAt }).ConfigureAwait(false);

        public async Task<IReadOnlyList<ReminderDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<ReminderDto>>(nameof(GetByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<ReminderDto>> GetDueAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<ReminderDto>>(nameof(GetDueAsync), cancellationToken, new object?[] { now }).ConfigureAwait(false);

        public async Task MarkSentAsync(Guid id, DateTimeOffset sentAt, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkSentAsync), cancellationToken, id, sentAt).ConfigureAwait(false);
    }
}
