namespace Vitarish.Appointments.Domain.Dto;

public record CapacityPolicyReadDto : EntityReadDto
{
    public Guid SessionId { get; set; }
    public int TotalSeats { get; set; }
    public bool PendingConsumesCapacity { get; set; }
    public bool WaitlistAllowed { get; set; }
    public int? MaxWaitlistSize { get; set; }
}
