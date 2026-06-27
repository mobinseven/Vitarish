using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Domain.Dto;

public record AssetReadDto : EntityReadDto
{
    public AssetType Type { get; set; }
    public Guid FileId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
}
