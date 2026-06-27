using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record AssetWriteDto : EntityWriteDto
{
    public AssetType Type { get; set; } = AssetType.Image;
    public Guid FileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
}
