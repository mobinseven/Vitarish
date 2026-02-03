namespace Vitarish.Cms.Application.Abstractions;

public interface IContentCommentsService : IEntityService<ContentCommentDto, ContentCommentWriteDto, ContentCommentDescriptorDto>
{
    Task ApproveAsync(Guid id, CancellationToken cancellationToken);
}
