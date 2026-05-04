namespace Vitarish.Calendar.Domain.Dto;

public record CalendarParticipantReadDto : EntityReadDto
{
    public Guid MappingId { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public string LocalReferenceId { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; }
}
