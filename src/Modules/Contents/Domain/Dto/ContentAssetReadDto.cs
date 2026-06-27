using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record ContentAssetReadDto : EntityReadDto
{
    public Guid ContentId { get; set; }
    public Guid AssetId { get; set; }
    public ContentAssetRole Role { get; set; }
    public int Position { get; set; }
}
