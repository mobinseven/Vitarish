namespace Vitarish.Sea.Domain.Dto;

public record EventWriteDto : EntityWriteDto
{
    public Guid ProjectId { get; set; }
    public Guid ProcedureId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid? PreviousEventId { get; set; }
    public DateTimeOffset? CommittedAt { get; set; }
    public Guid CommittedBy { get; set; }
}
