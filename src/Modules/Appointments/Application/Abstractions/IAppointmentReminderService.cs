using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Entities.Server.Models;

namespace Vitarish.Appointments.Application.Abstractions;

public interface IAppointmentReminderService
{
    Task<EntityResult<IReadOnlyList<ReminderReadDto>>> GetUserReservationRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken);

    Task<EntityResult<ReminderReadDto>> AddUserReservationReminderAsync(
        Guid reservationId,
        ReminderKind kind,
        DateTimeOffset remindAt,
        string? recipientUserId,
        string? recipient,
        CancellationToken cancellationToken);

    Task<EntityResult<ReminderReadDto>> UpdateUserReservationReminderAsync(
        Guid reservationId,
        Guid reminderId,
        ReminderKind kind,
        DateTimeOffset remindAt,
        string? recipientUserId,
        string? recipient,
        CancellationToken cancellationToken);

    Task<EntityResult> DeleteUserReservationReminderAsync(
        Guid reservationId,
        Guid reminderId,
        CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>> GetSessionReminderPoliciesAsync(
        Guid sessionId,
        CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>> SaveSessionReminderPoliciesAsync(
        Guid sessionId,
        IReadOnlyList<SessionReminderPolicyWriteDto> policies,
        CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<ReminderReadDto>>> MaterializeAutoRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken);
}
