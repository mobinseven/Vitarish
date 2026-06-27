using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record ContentAssetWriteDto : EntityWriteDto
{
    public Guid ContentId { get; set; }
    public Guid AssetId { get; set; }
    public ContentAssetRole Role { get; set; } = ContentAssetRole.Attachment;
    public int Position { get; set; }
}
