namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentReadDto : EntityReadDto
{
    public AppointmentStatus Status { get; set; }

    public Guid SessionId { get; set; }

    public string? UserId { get; set; }

    public AppointmentSessionReadDto? Session { get; set; }
}
