namespace Vitarish.Appointments.Domain.Dto;

public record ShiftBookingPolicyReadDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public DateOnly Date { get; set; }
    public int? BookingOpensBefore { get; set; }
    public int? BookingCloseBefore { get; set; }
    public bool Closed { get; set; }
    public int? Capacity { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan? CancellationDeadline { get; set; }
    public bool AllowRescheduling { get; set; }
    public int? MaxReservationsPerParty { get; set; }

    [JsonIgnore]
    public string Url => $"/shifts/{ShiftId}/booking-policies/{Id}";
}
