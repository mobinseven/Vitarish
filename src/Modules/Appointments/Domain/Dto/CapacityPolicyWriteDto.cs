namespace Vitarish.Appointments.Domain.Dto;

public record CapacityPolicyWriteDto : EntityWriteDto
{
    public Guid SessionId { get; set; }
    public int TotalSeats { get; set; } = 1;
    public bool PendingConsumesCapacity { get; set; } = true;
    public bool WaitlistAllowed { get; set; }
    public int? MaxWaitlistSize { get; set; }
}
