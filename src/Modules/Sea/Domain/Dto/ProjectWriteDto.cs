namespace Vitarish.Sea.Domain.Dto;

public record ProjectWriteDto : EntityWriteDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? AvatarFileId { get => GetField<Guid?>(); set => SetField(value); }
    public string? IdentificationCode { get; set; }
    public bool Template { get; set; }
    public IReadOnlyList<string>? UserIds { get; set; }
}
