using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class ReceiptsClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<ReceiptDto, ReceiptDto, ReceiptDescriptorDto, ReceiptsClientDefinition>(connectionFactory, entityInterceptionProvider),
            IReceiptsClient
    {
        public async Task<Guid> CreateReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<Guid>(nameof(CreateReceiptAsync), cancellationToken, new object?[] { userId, objectId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyList<Guid>>(nameof(GetReadersAsync), cancellationToken, new object?[] { objectId }).ConfigureAwait(false);

        public async Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkProjectReadAsync), cancellationToken, userId, projectId).ConfigureAwait(false);

        public async Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkProjectUnreadAsync), cancellationToken, userId, projectId).ConfigureAwait(false);

        public async Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkProcedureReadAsync), cancellationToken, userId, procedureId).ConfigureAwait(false);

        public async Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkProcedureUnreadAsync), cancellationToken, userId, procedureId).ConfigureAwait(false);

        public async Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkEventReadAsync), cancellationToken, userId, eventId).ConfigureAwait(false);

        public async Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(MarkEventUnreadAsync), cancellationToken, userId, eventId).ConfigureAwait(false);

        public async Task<bool> HasReceiptAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<bool>(nameof(HasReceiptAsync), cancellationToken, new object?[] { userId, objectId }).ConfigureAwait(false);

        public async Task<IReadOnlyDictionary<Guid, bool>> HasReceiptsAsync(Guid userId, IEnumerable<Guid> objectIds, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<IReadOnlyDictionary<Guid, bool>>(nameof(HasReceiptsAsync), cancellationToken, new object?[] { userId, objectIds }).ConfigureAwait(false);
    }
}
