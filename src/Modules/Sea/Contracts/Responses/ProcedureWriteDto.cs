namespace Vitarish.Sea.Contracts.Responses;

public record ProcedureWriteDto : EntityWriteDto
{
    public Guid ProjectId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
