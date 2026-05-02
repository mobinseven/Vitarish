namespace Vitarish.Appointments.Domain.Dto;

public record AppointmentWriteDto : EntityWriteDto
{
    public AppointmentStatus Status { get; set; }

    public Guid SessionId { get; set; }

    public string? UserId { get; set; }
}
