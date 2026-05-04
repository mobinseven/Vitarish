namespace Vitarish.Appointments.Domain.Dto;

public record SessionReadDto : EntityReadDto
{
    public string Title { get; set; } = string.Empty;
    public string? OwnerId { get; set; }
    public SessionStatus Status { get; set; }
    public SessionVisibility Visibility { get; set; }
    public Guid? CalendarMappingId { get; set; }
    public SessionStats Stats { get; set; } = new();
}
