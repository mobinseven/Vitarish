using MediatR;

namespace Vitarish.Appointments.Domain.Events;

public sealed record ShiftBookingPolicyCreated(Guid PolicyId) : INotification;
public sealed record ShiftBookingPolicyUpdated(Guid PolicyId) : INotification;
public sealed record ShiftBookingPolicyRemoved(Guid PolicyId) : INotification;
