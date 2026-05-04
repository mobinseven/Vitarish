namespace Vitarish.Appointments.Domain.Dto;

public record SessionWriteDto : EntityWriteDto
{
    public string Title { get; set; } = string.Empty;
    public string? OwnerId { get; set; }
    public SessionStatus Status { get; set; }
    public SessionVisibility Visibility { get; set; }
    public Guid? CalendarMappingId { get; set; }
}
