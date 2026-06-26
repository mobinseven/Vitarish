namespace Vitarish.Sea.Contracts.Responses;

public record EventWriteDto : EntityWriteDto
{
    public Guid ProjectId { get; set; }
    public Guid ProcedureId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public Guid? PreviousEventId { get; set; }
    public DateTime? CommittedAt { get; set; }
    public Guid CommittedBy { get; set; }
}
