using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Dto;
using Vorn.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Services
{
    internal sealed class AttachmentsClient(
        IIdentityClientSignalRFactory connectionFactory,
        IEntityInterceptionProvider entityInterceptionProvider)
        : SeaEntityClient<AttachmentDto, AttachmentDescriptorDto, AttachmentsClientDefinition>(connectionFactory, entityInterceptionProvider),
            IAttachmentsClient
    {
        public async Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default) => await InvokeHubAsync<IReadOnlyList<AttachmentDto>>(nameof(GetByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<int>(nameof(GetCountByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<AttachmentDto?> AddAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<AttachmentDto?>(nameof(AddAsync), cancellationToken, new object?[] { eventId, content, contentType, contentThumbnail, size }).ConfigureAwait(false);

        public async Task AddAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(AddAsync), cancellationToken, eventId, attachments).ConfigureAwait(false);

        public async Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken = default) =>
            await InvokeNoInterceptAsync(nameof(RemoveAsync), cancellationToken, eventId, attachmentId).ConfigureAwait(false);

        public async Task<AttachmentDto?> UpdateAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<AttachmentDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { eventId, attachmentId, content }).ConfigureAwait(false);

        public async Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeHubAsync<AttachmentSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
