namespace Vitarish.Cms.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentCommentsService(IMediator mediator, ContentCommentsMapper mapper)
    : EntityService<ContentComment, ContentCommentDescriptor, ContentCommentDto, ContentCommentWriteDto, ContentCommentDescriptorDto>(
        mediator,
        mapper.ToDto,
        mapper.ToDomain,
        mapper.ToDomain),
      IContentCommentsService
{
    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        ContentComment? comment = (await GetEntityAsync(id, cancellationToken)).GetValueOrDefault();
        if (comment is null)
        {
            return;
        }

        comment.Approve();

        _ = (await UpdateEntityAsync(comment, cancellationToken)).EnsureValue("Approve content comment");
    }
}
