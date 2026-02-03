namespace Vitarish.Cms.Infrastructure.Mapping;

[RegisterTransient]
internal sealed class ContentCommentsMapper
{
    public ContentCommentDto ToDto(ContentComment src)
        => new(
            src.ContentId,
            src.ParentCommentId,
            src.AuthorName,
            src.AuthorId,
            src.Body,
            src.IsApproved)
        {
            Id = src.Id,
            CreatedAt = src.CreatedAt,
            CreatedBy = src.CreatedBy,
            UpdatedAt = src.UpdatedAt,
            UpdatedBy = src.UpdatedBy,
            DeletedAt = src.DeletedAt,
            DeletedBy = src.DeletedBy
        };

    public ContentComment ToDomain(ContentCommentWriteDto src)
    {
        ContentComment entity = new(src.ContentId, src.AuthorName, src.Body, src.ParentCommentId, src.AuthorId)
        {
            Id = src.Id
        };

        if (src.IsApproved)
        {
            entity.Approve();
        }
        else
        {
            entity.Reject();
        }

        return entity;
    }

    public ContentCommentDescriptor ToDomain(ContentCommentDescriptorDto src) => new()
    {
        Id = src.Id,
        Skip = src.Skip,
        Take = src.Take,
        Search = src.Search,
        CreatedFrom = src.CreatedFrom,
        CreatedTo = src.CreatedTo,
        CreatedBy = src.CreatedBy,
        UpdatedFrom = src.UpdatedFrom,
        UpdatedTo = src.UpdatedTo,
        UpdatedBy = src.UpdatedBy,
        DeletedFrom = src.DeletedFrom,
        DeletedTo = src.DeletedTo,
        DeletedBy = src.DeletedBy,
        ContentId = src.ContentId,
        IsApproved = src.IsApproved
    };
}
