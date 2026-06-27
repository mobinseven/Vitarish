using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Contracts.Responses;

public record ProjectWriteDto : EntityWriteDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? IdentificationCode { get; set; }
    public bool Template { get; set; }
    public IReadOnlyList<string>? UserIds { get; set; }
    public Guid? AvatarFileId { get; set; }
}
