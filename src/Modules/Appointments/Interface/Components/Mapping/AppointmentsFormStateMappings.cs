using Vitarish.Appointments.Contracts.Dto;

namespace Vitarish.Appointments.Interface.Components.Mapping;

public static class AppointmentsFormStateMappings
{
    public static ReservationWriteDto ToWrite(this ReservationReadDto dto) => new() { Id = dto.Id, ShiftId = dto.ShiftId, Date = dto.Date, Status = dto.Status, ReservingPartyId = dto.ReservingPartyId, EstimatedSessionLength = dto.EstimatedSessionLength };
    public static ServiceWriteDto ToWrite(this ServiceReadDto dto) => new() { Id = dto.Id, ServiceTypeId = dto.ServiceTypeId, Name = dto.Name, Description = dto.Description };
    public static ServiceProviderWriteDto ToWrite(this ServiceProviderReadDto dto) => new() { Id = dto.Id, Name = dto.Name, Description = dto.Description, LinkedUserIds = dto.LinkedUserIds.ToList() };
    public static ServiceTypeWriteDto ToWrite(this ServiceTypeReadDto dto) => new() { Id = dto.Id, Name = dto.Name, Description = dto.Description };
    public static ShiftWriteDto ToWrite(this ShiftReadDto dto) => new() { Id = dto.Id, ServiceProviderId = dto.ServiceProviderId, ServiceId = dto.ServiceId, DayOfWeek = dto.DayOfWeek, StartTime = dto.StartTime, EndTime = dto.EndTime, TotalSeats = dto.TotalSeats, TimeZone = dto.TimeZone, Status = dto.Status, Visibility = dto.Visibility, BookingCloseBefore = dto.BookingCloseBefore, BookingOpensBefore = dto.BookingOpensBefore };
    public static ShiftReminderPolicyWriteDto ToWrite(this ShiftReminderPolicyReadDto dto) => new() { Id = dto.Id, ShiftId = dto.ShiftId, OffsetBefore = dto.OffsetBefore, Channel = dto.Channel, SubjectTemplate = dto.SubjectTemplate, BodyTemplate = dto.BodyTemplate, QualifyingStatuses = dto.QualifyingStatuses, IsEnabled = dto.IsEnabled };
    public static ShiftBookingPolicyWriteDto ToWrite(this ShiftBookingPolicyReadDto dto) => new() { Id = dto.Id, ShiftId = dto.ShiftId, Date = dto.Date, BookingOpensBefore = dto.BookingOpensBefore, BookingCloseBefore = dto.BookingCloseBefore, Closed = dto.Closed, Capacity = dto.Capacity, RequiresApproval = dto.RequiresApproval, CancellationDeadline = dto.CancellationDeadline, EstimatedSessionLength = dto.EstimatedSessionLength, AllowRescheduling = dto.AllowRescheduling, MaxReservationsPerParty = dto.MaxReservationsPerParty };
}
