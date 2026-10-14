using Vitarish.Appointments.Contracts.Enums;

namespace Vitarish.Clinic.Contracts.Responses;

public sealed record DashboardDateRangeRequest(DateOnly dateFrom, DateOnly dateTo, int? take = null);

public sealed record DashboardAppointmentResponse(
    Guid ReservationId,
    string? PatientId,
    string? PatientDisplayName,
    Guid? ServiceId,
    string? ServiceDisplayName,
    DateOnly Date,
    TimeOnly? StartTime,
    string? TimeZone,
    ReservationStatus Status,
    IReadOnlyList<string> PatientTags);
