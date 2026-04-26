namespace Vitarish.Sea.Domain.Dto;

public record ProcedureWriteDto : EntityWriteDto
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; }
    public string? Description { get => GetField<string>(); set => SetField(value); }
}
