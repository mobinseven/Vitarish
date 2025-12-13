using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions
{
    public interface IAttachmentsClient
    {
        Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
        Task<AttachmentDto?> AddAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken = default);
        Task AddAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken = default);
        Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken = default);
        Task<AttachmentDto?> UpdateAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken = default);
        Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
    }
}

namespace Vitarish.Clients.Modules.Sea.Internal.Services
{
    internal sealed class AttachmentsClient(IIdentityClientSignalRFactory connectionFactory)
        : SeaHubClient<AttachmentsClientDefinition>(connectionFactory),
            IAttachmentsClient
    {
        public async Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<IReadOnlyList<AttachmentDto>>(nameof(GetByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default) =>
            await InvokeAsync<int>(nameof(GetCountByEventAsync), cancellationToken, new object?[] { eventId }).ConfigureAwait(false);

        public async Task<AttachmentDto?> AddAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken = default) =>
            await InvokeAsync<AttachmentDto?>(nameof(AddAsync), cancellationToken, new object?[] { eventId, content, contentType, contentThumbnail, size }).ConfigureAwait(false);

        public async Task AddAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(AddAsync), cancellationToken, new object?[] { eventId, attachments }).ConfigureAwait(false);

        public async Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken = default) =>
            await InvokeAsync(nameof(RemoveAsync), cancellationToken, new object?[] { eventId, attachmentId }).ConfigureAwait(false);

        public async Task<AttachmentDto?> UpdateAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken = default) =>
            await InvokeAsync<AttachmentDto?>(nameof(UpdateAsync), cancellationToken, new object?[] { eventId, attachmentId, content }).ConfigureAwait(false);

        public async Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default) =>
            await InvokeAsync<AttachmentSnapshot?>(nameof(CaptureSnapshotAsync), cancellationToken, new object?[] { id }).ConfigureAwait(false);
    }
}
