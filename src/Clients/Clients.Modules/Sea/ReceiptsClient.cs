using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IReceiptsClient
    {
        Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken = default);
        Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
        Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default);
        Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default);
        Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default);
        Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
        Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default);
        Task<bool> GetAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class ReceiptsClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<ReceiptsClientDefinition>(connectionFactory),
            IReceiptsClient
    {
        public async Task<Guid> AddAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<Guid>(nameof(AddAsync), cancellationToken, new object?[] { userId, objectId }).ConfigureAwait(false);

        public async Task<IReadOnlyList<Guid>> GetReadersAsync(Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<Guid>>(nameof(GetReadersAsync), cancellationToken, new object?[] { objectId }).ConfigureAwait(false);

        public async Task MarkProjectReadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkProjectReadAsync), cancellationToken, new object?[] { userId, projectId }).ConfigureAwait(false);

        public async Task MarkProjectUnreadAsync(Guid userId, Guid projectId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkProjectUnreadAsync), cancellationToken, new object?[] { userId, projectId }).ConfigureAwait(false);

        public async Task MarkProcedureReadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkProcedureReadAsync), cancellationToken, new object?[] { userId, procedureId }).ConfigureAwait(false);

        public async Task MarkProcedureUnreadAsync(Guid userId, Guid procedureId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkProcedureUnreadAsync), cancellationToken, new object?[] { userId, procedureId }).ConfigureAwait(false);

        public async Task MarkEventReadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkEventReadAsync), cancellationToken, new object?[] { userId, eventId }).ConfigureAwait(false);

        public async Task MarkEventUnreadAsync(Guid userId, Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(MarkEventUnreadAsync), cancellationToken, new object?[] { userId, eventId }).ConfigureAwait(false);

        public async Task<bool> GetAsync(Guid userId, Guid objectId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<bool>(nameof(GetAsync), cancellationToken, new object?[] { userId, objectId }).ConfigureAwait(false);
    }
}
