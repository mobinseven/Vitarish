namespace Vitarish.Clinic.Contracts.Responses;

/// <summary>The practice-local calendar date and how many appointments fall on it.</summary>
public sealed record TodayAppointmentCountResponse(DateOnly Date, int Count);
