using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Dto;
using Vitarish.Communications.Domain.Enums;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Services;

internal sealed class AppointmentReminderService(
    IAccessControlledReservationManager reservationManager,
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    ISessionReminderPolicyManager policyManager,
    IEntityRepository<Session, SessionDescriptor> sessionRepository,
    IEntityRepository<SessionOccurrence, SessionOccurrenceDescriptor> occurrenceRepository,
    IRequestContext requestContext,
    IRemindersService remindersService) : IAppointmentReminderService
{
    private const string DefaultSubjectTemplate = "Appointment reminder";
    private const string DefaultBodyTemplate = "You have an appointment reservation starting at {OccurrenceStart}.";

    private bool IsAdmin => requestContext.IsTenantAdmin() || requestContext.IsTenantOwner();

    private string? UserId => requestContext.UserId;

    public async Task<EntityResult<IReadOnlyList<ReminderReadDto>>> GetUserReservationRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> reservationResult = await GetManageableReservationAsync(reservationId, cancellationToken);
        if (reservationResult.IsFailure)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure([.. reservationResult.Errors]);
        }

        IReadOnlyList<ReminderReadDto> reminders = await remindersService.GetBySourceAsync(reservationId, cancellationToken);
        return EntityResult<IReadOnlyList<ReminderReadDto>>.Success(FilterRemindersForCurrentUser(reminders));
    }

    public async Task<EntityResult<ReminderReadDto>> AddUserReservationReminderAsync(
        Guid reservationId,
        ReminderKind kind,
        DateTimeOffset remindAt,
        string? recipientUserId,
        string? recipient,
        CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> reservationResult = await GetManageableReservationAsync(reservationId, cancellationToken);
        if (reservationResult.IsFailure || reservationResult.Value is null)
        {
            return EntityResult<ReminderReadDto>.Failure([.. reservationResult.Errors]);
        }

        EntityResult validation = await ValidateReservationReminderAsync(reservationResult.Value, remindAt, requireNonTerminal: true, cancellationToken);
        if (validation.IsFailure)
        {
            return EntityResult<ReminderReadDto>.Failure([.. validation.Errors]);
        }

        EntityResult<ReminderRecipientReadDto> recipientResult = BuildRecipient(kind, recipientUserId, recipient);
        if (recipientResult.IsFailure || recipientResult.Value is null)
        {
            return EntityResult<ReminderReadDto>.Failure([.. recipientResult.Errors]);
        }

        IReadOnlyList<ReminderReadDto> added = await remindersService.AddRangeAsync(
            reservationId,
            [recipientResult.Value],
            BuildSubject(reservationResult.Value, null),
            BuildBody(reservationResult.Value, null),
            remindAt,
            cancellationToken);

        ReminderReadDto? reminder = added.FirstOrDefault();
        return reminder is null
            ? EntityResult<ReminderReadDto>.Failure(EntityError.Unknown("Reminder was not returned after creation."))
            : EntityResult<ReminderReadDto>.Success(reminder);
    }

    public async Task<EntityResult<ReminderReadDto>> UpdateUserReservationReminderAsync(
        Guid reservationId,
        Guid reminderId,
        ReminderKind kind,
        DateTimeOffset remindAt,
        string? recipientUserId,
        string? recipient,
        CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> reservationResult = await GetManageableReservationAsync(reservationId, cancellationToken);
        if (reservationResult.IsFailure || reservationResult.Value is null)
        {
            return EntityResult<ReminderReadDto>.Failure([.. reservationResult.Errors]);
        }

        EntityResult<ReminderReadDto> reminderResult = await GetReminderForReservationAsync(reservationId, reminderId, cancellationToken);
        if (reminderResult.IsFailure || reminderResult.Value is null)
        {
            return EntityResult<ReminderReadDto>.Failure([.. reminderResult.Errors]);
        }

        EntityResult validation = await ValidateReservationReminderAsync(reservationResult.Value, remindAt, requireNonTerminal: true, cancellationToken);
        if (validation.IsFailure)
        {
            return EntityResult<ReminderReadDto>.Failure([.. validation.Errors]);
        }

        EntityResult<ReminderRecipientReadDto> recipientResult = BuildRecipient(kind, recipientUserId, recipient);
        if (recipientResult.IsFailure || recipientResult.Value is null)
        {
            return EntityResult<ReminderReadDto>.Failure([.. recipientResult.Errors]);
        }

        ReminderReadDto? updated = await remindersService.UpdateAsync(
            reminderId,
            kind,
            recipientResult.Value.RecipientUserId ?? string.Empty,
            recipientResult.Value.Recipient,
            BuildSubject(reservationResult.Value, null),
            BuildBody(reservationResult.Value, null),
            remindAt,
            cancellationToken);

        return updated is null
            ? EntityResult<ReminderReadDto>.Failure(EntityError.NotFound("Reminder not found.", nameof(reminderId)))
            : EntityResult<ReminderReadDto>.Success(updated);
    }

    public async Task<EntityResult> DeleteUserReservationReminderAsync(
        Guid reservationId,
        Guid reminderId,
        CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> reservationResult = await GetManageableReservationAsync(reservationId, cancellationToken);
        if (reservationResult.IsFailure)
        {
            return EntityResult.Failure([.. reservationResult.Errors]);
        }

        EntityResult<ReminderReadDto> reminderResult = await GetReminderForReservationAsync(reservationId, reminderId, cancellationToken);
        if (reminderResult.IsFailure)
        {
            return EntityResult.Failure([.. reminderResult.Errors]);
        }

        return await remindersService.RemoveAsync(reminderId, cancellationToken);
    }

    public async Task<EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>> GetSessionReminderPoliciesAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        EntityResult adminResult = await EnsureCanManageSessionPoliciesAsync(sessionId, cancellationToken);
        if (adminResult.IsFailure)
        {
            return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure([.. adminResult.Errors]);
        }

        return await policyManager.GetListAsync(new SessionReminderPolicyDescriptorDto
        {
            SessionId = sessionId,
            Skip = 0,
            Take = int.MaxValue
        }, cancellationToken);
    }

    public async Task<EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>> SaveSessionReminderPoliciesAsync(
        Guid sessionId,
        IReadOnlyList<SessionReminderPolicyWriteDto> policies,
        CancellationToken cancellationToken)
    {
        EntityResult adminResult = await EnsureCanManageSessionPoliciesAsync(sessionId, cancellationToken);
        if (adminResult.IsFailure)
        {
            return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure([.. adminResult.Errors]);
        }

        EntityResult validation = ValidatePolicies(sessionId, policies);
        if (validation.IsFailure)
        {
            return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure([.. validation.Errors]);
        }

        EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>> existingResult = await policyManager.GetListAsync(
            new SessionReminderPolicyDescriptorDto { SessionId = sessionId, Skip = 0, Take = int.MaxValue },
            cancellationToken);
        if (existingResult.IsFailure || existingResult.Value is null)
        {
            return existingResult;
        }

        HashSet<Guid> existingIds = [.. existingResult.Value.Select(p => p.Id)];
        if (policies.Any(p => p.Id != Guid.Empty && !existingIds.Contains(p.Id)))
        {
            return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure(EntityError.NotFound("Session reminder policy not found for this session.", nameof(SessionReminderPolicyWriteDto.Id)));
        }

        foreach (SessionReminderPolicyReadDto existing in existingResult.Value)
        {
            if (!policies.Any(p => p.Id == existing.Id))
            {
                EntityResult removeResult = await policyManager.RemoveAsync(existing.Id, cancellationToken);
                if (removeResult.IsFailure)
                {
                    return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure([.. removeResult.Errors]);
                }
            }
        }

        foreach (SessionReminderPolicyWriteDto policy in policies)
        {
            SessionReminderPolicyWriteDto normalized = policy with { SessionId = sessionId };
            EntityResult<SessionReminderPolicyReadDto> writeResult = normalized.Id == Guid.Empty
                ? await policyManager.AddAsync(normalized, cancellationToken)
                : await policyManager.UpdateAsync(normalized, cancellationToken);
            if (writeResult.IsFailure)
            {
                return EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>>.Failure([.. writeResult.Errors]);
            }
        }

        return await policyManager.GetListAsync(new SessionReminderPolicyDescriptorDto
        {
            SessionId = sessionId,
            Skip = 0,
            Take = int.MaxValue
        }, cancellationToken);
    }

    public async Task<EntityResult<IReadOnlyList<ReminderReadDto>>> MaterializeAutoRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> reservationResult = await GetManageableReservationAsync(reservationId, cancellationToken);
        if (reservationResult.IsFailure || reservationResult.Value is null)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure([.. reservationResult.Errors]);
        }

        ReservationReadDto reservation = reservationResult.Value;
        EntityResult validation = await ValidateReservationReminderAsync(reservation, DateTimeOffset.UtcNow.AddTicks(1), requireNonTerminal: true, cancellationToken);
        if (validation.IsFailure)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure([.. validation.Errors]);
        }

        if (reservation.Occurrence is null)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure(EntityError.Validation("Reservation occurrence is required.", nameof(reservation.Occurrence)));
        }

        EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>> policiesResult = await policyManager.GetListAsync(
            new SessionReminderPolicyDescriptorDto
            {
                SessionId = reservation.Occurrence.SessionId,
                IsEnabled = true,
                Skip = 0,
                Take = int.MaxValue
            },
            cancellationToken);
        if (policiesResult.IsFailure || policiesResult.Value is null)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure([.. policiesResult.Errors]);
        }

        IReadOnlyList<ReminderReadDto> existingReminders = await remindersService.GetBySourceAsync(reservationId, cancellationToken);
        HashSet<(ReminderKind Kind, DateTimeOffset RemindAt, string RecipientUserId, string Recipient)> materializedReminderKeys = [
            .. existingReminders.Select(static r => (r.Kind, r.RemindAt, r.RecipientUserId, r.Recipient))
        ];
        List<ReminderRecipientReadDto> recipients = [];
        List<(SessionReminderPolicyReadDto Policy, DateTimeOffset RemindAt)> materializablePolicies = [];
        foreach (SessionReminderPolicyReadDto policy in policiesResult.Value.Where(p => p.QualifyingReservationStatuses.Contains(reservation.Status)))
        {
            DateTimeOffset remindAt = reservation.Occurrence.OccurrenceStart - policy.OffsetBeforeOccurrenceStart;
            ReminderKind reminderKind = ToReminderKind(policy.Kind);
            if (remindAt <= DateTimeOffset.UtcNow || remindAt >= reservation.Occurrence.OccurrenceStart)
            {
                continue;
            }

            EntityResult<ReminderRecipientReadDto> recipientResult = BuildRecipient(reminderKind, reservation.ReservingPartyId, null);
            if (recipientResult.IsSuccess
                && recipientResult.Value is not null
                && materializedReminderKeys.Add((reminderKind, remindAt, recipientResult.Value.RecipientUserId, recipientResult.Value.Recipient)))
            {
                recipients.Add(recipientResult.Value);
                materializablePolicies.Add((policy, remindAt));
            }
        }

        List<ReminderReadDto> reminders = [];
        for (int i = 0; i < materializablePolicies.Count; i++)
        {
            SessionReminderPolicyReadDto policy = materializablePolicies[i].Policy;
            IReadOnlyList<ReminderReadDto> added = await remindersService.AddRangeAsync(
                reservationId,
                [recipients[i]],
                BuildSubject(reservation, policy),
                BuildBody(reservation, policy),
                materializablePolicies[i].RemindAt,
                cancellationToken);
            reminders.AddRange(added);
        }

        return EntityResult<IReadOnlyList<ReminderReadDto>>.Success(reminders);
    }

    public async Task<EntityResult> RemoveUnsentReservationRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ReminderReadDto> reminders = await remindersService.GetBySourceAsync(reservationId, cancellationToken);
        foreach (ReminderReadDto reminder in reminders.Where(static r => !r.IsSent))
        {
            EntityResult removeResult = await remindersService.RemoveAsync(reminder.Id, cancellationToken);
            if (removeResult.IsFailure)
            {
                return EntityResult.Failure([.. removeResult.Errors]);
            }
        }

        return EntityResult.Success();
    }

    public async Task<EntityResult<IReadOnlyList<ReminderReadDto>>> RecalculateUnsentReservationRemindersAsync(
        Guid reservationId,
        CancellationToken cancellationToken)
    {
        EntityResult removeResult = await RemoveUnsentReservationRemindersAsync(reservationId, cancellationToken);
        if (removeResult.IsFailure)
        {
            return EntityResult<IReadOnlyList<ReminderReadDto>>.Failure([.. removeResult.Errors]);
        }

        return await MaterializeAutoRemindersAsync(reservationId, cancellationToken);
    }

    public async Task<EntityResult> RemoveUnsentSessionRemindersAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SessionOccurrence> occurrences = await occurrenceRepository.GetAsync(
            new SessionOccurrenceDescriptor
            {
                SessionId = sessionId,
                Skip = 0,
                Take = int.MaxValue
            },
            cancellationToken);

        foreach (Guid occurrenceId in occurrences.Select(static occurrence => occurrence.Id))
        {
            IReadOnlyList<Reservation> reservations = await reservationRepository.GetAsync(
                new ReservationDescriptor
                {
                    SessionOccurrenceId = occurrenceId,
                    Skip = 0,
                    Take = int.MaxValue
                },
                cancellationToken);

            foreach (Reservation reservation in reservations)
            {
                EntityResult removeResult = await RemoveUnsentReservationRemindersAsync(reservation.Id, cancellationToken);
                if (removeResult.IsFailure)
                {
                    return removeResult;
                }
            }
        }

        return EntityResult.Success();
    }

    private async Task<EntityResult<ReservationReadDto>> GetManageableReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        EntityResult<ReservationReadDto> result = await reservationManager.GetAsync(reservationId, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return result.IsFailure
                ? result
                : EntityResult<ReservationReadDto>.Failure(EntityError.NotFound("Reservation not found.", nameof(reservationId)));
        }

        if (!IsAdmin && result.Value.ReservingPartyId != UserId)
        {
            return EntityResult<ReservationReadDto>.Failure(EntityError.NotFound("Reservation not found.", nameof(reservationId)));
        }

        return result;
    }

    private async Task<EntityResult<ReminderReadDto>> GetReminderForReservationAsync(Guid reservationId, Guid reminderId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ReminderReadDto> reminders = await remindersService.GetBySourceAsync(reservationId, cancellationToken);
        ReminderReadDto? reminder = FilterRemindersForCurrentUser(reminders).FirstOrDefault(r => r.Id == reminderId);
        return reminder is null
            ? EntityResult<ReminderReadDto>.Failure(EntityError.NotFound("Reminder not found.", nameof(reminderId)))
            : EntityResult<ReminderReadDto>.Success(reminder);
    }

    private IReadOnlyList<ReminderReadDto> FilterRemindersForCurrentUser(IReadOnlyList<ReminderReadDto> reminders)
        => IsAdmin ? reminders : [.. reminders.Where(r => r.RecipientUserId == UserId)];

    private EntityResult<ReminderRecipientReadDto> BuildRecipient(ReminderKind kind, string? recipientUserId, string? recipient)
    {
        string? effectiveUserId = IsAdmin ? recipientUserId : UserId;
        if (string.IsNullOrWhiteSpace(effectiveUserId))
        {
            return EntityResult<ReminderRecipientReadDto>.Failure(EntityError.Validation("Reminder recipient user is required.", nameof(recipientUserId)));
        }

        if (!IsAdmin && effectiveUserId != UserId)
        {
            return EntityResult<ReminderRecipientReadDto>.Failure(EntityError.Validation("Reminder recipient must be the current user.", nameof(recipientUserId)));
        }

        string? effectiveRecipient = IsAdmin
            ? recipient ?? (effectiveUserId == UserId ? GetCurrentUserAddress(kind) : null)
            : GetCurrentUserAddress(kind);
        if (string.IsNullOrWhiteSpace(effectiveRecipient))
        {
            return EntityResult<ReminderRecipientReadDto>.Failure(EntityError.Validation("Reminder recipient address is required.", nameof(recipient)));
        }

        return EntityResult<ReminderRecipientReadDto>.Success(new ReminderRecipientReadDto
        {
            Kind = kind,
            RecipientUserId = effectiveUserId,
            Recipient = effectiveRecipient
        });
    }

    private string? GetCurrentUserAddress(ReminderKind kind) => kind switch
    {
        ReminderKind.Email => requestContext.User?.GetUserEmail(),
        ReminderKind.Sms => requestContext.User?.GetUserPhoneNumber(),
        _ => null
    };

    private async Task<EntityResult> EnsureCanManageSessionPoliciesAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!IsAdmin)
        {
            return EntityResult.Failure(EntityError.Conflict("Tenant admin or owner role is required to manage session reminder policies."));
        }

        Session? session = await sessionRepository.GetAsync(sessionId, cancellationToken);
        return session is null
            ? EntityResult.Failure(EntityError.NotFound("Session not found.", nameof(sessionId)))
            : EntityResult.Success();
    }

    private async Task<EntityResult> ValidateReservationReminderAsync(ReservationReadDto reservation, DateTimeOffset remindAt, bool requireNonTerminal, CancellationToken cancellationToken)
    {
        if (reservation.Occurrence is null)
        {
            return EntityResult.Failure(EntityError.Validation("Reservation occurrence is required.", nameof(reservation.Occurrence)));
        }

        if (remindAt <= DateTimeOffset.UtcNow)
        {
            return EntityResult.Failure(EntityError.Validation("Reminder time must be in the future.", nameof(remindAt)));
        }

        if (remindAt >= reservation.Occurrence.OccurrenceStart)
        {
            return EntityResult.Failure(EntityError.Validation("Reminder time must be before the reservation occurrence start.", nameof(remindAt)));
        }

        if (requireNonTerminal && IsTerminal(reservation.Status))
        {
            return EntityResult.Failure(EntityError.Conflict("Terminal reservations do not accept new reminders.", nameof(reservation.Status)));
        }

        SessionOccurrence? occurrence = await occurrenceRepository.GetAsync(reservation.Occurrence.Id, cancellationToken);
        if (occurrence is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Reservation occurrence not found.", nameof(reservation.Occurrence)));
        }

        if (requireNonTerminal && (occurrence.IsCancelled || IsTerminal(occurrence.Session?.Status)))
        {
            return EntityResult.Failure(EntityError.Conflict("Terminal sessions do not accept new reminders."));
        }

        return EntityResult.Success();
    }

    private static EntityResult ValidatePolicies(Guid sessionId, IReadOnlyList<SessionReminderPolicyWriteDto> policies)
    {
        if (policies is null)
        {
            return EntityResult.Failure(EntityError.Validation("Reminder policies are required.", nameof(policies)));
        }

        if (policies.Any(p => p.SessionId != Guid.Empty && p.SessionId != sessionId))
        {
            return EntityResult.Failure(EntityError.Validation("All reminder policies must belong to the session being saved.", nameof(SessionReminderPolicyWriteDto.SessionId)));
        }

        if (policies.Any(p => p.OffsetBeforeOccurrenceStart <= TimeSpan.Zero))
        {
            return EntityResult.Failure(EntityError.Validation("Reminder policy offset must be greater than zero.", nameof(SessionReminderPolicyWriteDto.OffsetBeforeOccurrenceStart)));
        }

        return EntityResult.Success();
    }

    private static bool IsTerminal(ReservationStatus status) => status is ReservationStatus.Cancelled
        or ReservationStatus.Rejected
        or ReservationStatus.Expired
        or ReservationStatus.Completed;

    private static bool IsTerminal(SessionStatus? status) => status is SessionStatus.Closed
        or SessionStatus.Cancelled
        or SessionStatus.Archived;

    private static ReminderKind ToReminderKind(SessionReminderKind kind) => kind switch
    {
        SessionReminderKind.Email => ReminderKind.Email,
        SessionReminderKind.Sms => ReminderKind.Sms,
        _ => ReminderKind.Email
    };

    private static string BuildSubject(ReservationReadDto reservation, SessionReminderPolicyReadDto? policy)
        => ApplyTemplate(string.IsNullOrWhiteSpace(policy?.SubjectTemplate) ? DefaultSubjectTemplate : policy.SubjectTemplate, reservation);

    private static string BuildBody(ReservationReadDto reservation, SessionReminderPolicyReadDto? policy)
        => ApplyTemplate(string.IsNullOrWhiteSpace(policy?.BodyTemplate) ? DefaultBodyTemplate : policy.BodyTemplate, reservation);

    private static string ApplyTemplate(string template, ReservationReadDto reservation)
        => template
            .Replace("{ReservationId}", reservation.Id.ToString(), StringComparison.Ordinal)
            .Replace("{OccurrenceStart}", reservation.Occurrence?.OccurrenceStart.ToString("u") ?? string.Empty, StringComparison.Ordinal)
            .Replace("{OccurrenceEnd}", reservation.Occurrence?.OccurrenceEnd.ToString("u") ?? string.Empty, StringComparison.Ordinal);
}
