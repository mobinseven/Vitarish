using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Appointments.Application.Events;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Entities.Server.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class CreateReservationHandler(
    AppointmentsDbContext dbContext,
    IAppointmentsEventPublisher eventPublisher,
    IPublisher publisher,
    IRequestContext requestContext)
    : IRequestHandler<CreateReservationCommand, EntityResult<Guid>>
{
    public async Task<EntityResult<Guid>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();
        EntityResult<Reservation> createResult = await executionStrategy.ExecuteAsync(
            async () =>
            {
                await using IDbContextTransaction transaction = await dbContext.Database
                    .BeginTransactionAsync(cancellationToken);

                Task<Shift?> shiftTask = dbContext.Shifts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.ShiftId, cancellationToken);
                Task<IReadOnlyList<ShiftBookingPolicy>> policiesTask = LoadPoliciesAsync(request.ShiftId, cancellationToken);

                await Task.WhenAll(shiftTask, policiesTask);

                Shift? shift = await shiftTask;
                IReadOnlyList<ShiftBookingPolicy> policies = shift is not null
                    ? await policiesTask
                    : [];

                EntityResult shiftValidation = ValidateShift(shift, request.Date, policies);
                if (shiftValidation.IsFailure)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure([.. shiftValidation.Errors]);
                }

                if (!ShiftBookingPolicyEvaluator.IsBookingOpen(shift!, request.Date, policies))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("The booking window is closed for this shift slot."));
                }

                if (request.EstimatedSessionLength is { } estimatedSessionLength && estimatedSessionLength <= TimeSpan.Zero)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Validation(
                        "Estimated session length must be greater than zero.",
                        new Dictionary<string, string> { [nameof(request.EstimatedSessionLength)] = "Estimated session length must be greater than zero." }));
                }

                if (!request.CapacityOverride &&
                    !await HasCapacityForPartyAsync(shift!, request.Date, request.ReservingPartyId, policies, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("The reservation limit for this party has been reached."));
                }

                string? tenantId = requestContext.TenantId;

                if (tenantId is not null)
                {
                    ShiftReservationLock lockEntry = new()
                    {
                        ShiftId = request.ShiftId,
                        Date = request.Date,
                        TenantId = tenantId
                    };

                    _ = dbContext.ShiftReservationLocks.Add(lockEntry);
                    try
                    {
                        _ = await dbContext.SaveChangesAsync(cancellationToken);
                    }
                    catch (DbUpdateException lockEx) when (DatabaseExceptions.IsUniqueConstraintViolation(lockEx))
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Concurrency("This slot is temporarily locked. Please try again."));
                    }

                    int occupiedSeats = await CountActiveReservationsAsync(request.ShiftId, request.Date, cancellationToken);
                    if (!request.CapacityOverride && occupiedSeats >= ShiftBookingPolicyEvaluator.GetEffectiveCapacity(shift!, request.Date, policies))
                    {
                        _ = dbContext.ShiftReservationLocks.Remove(lockEntry);
                        _ = await dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Conflict("No seats are available for this shift slot."));
                    }

                    Reservation reservation = BuildReservation(shift!, request, policies);

                    _ = dbContext.Reservations.Add(reservation);
                    _ = dbContext.ShiftReservationLocks.Remove(lockEntry);

                    try
                    {
                        _ = await dbContext.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        return EntityResult<Reservation>.Success(reservation);
                    }
                    catch (DbUpdateException)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return EntityResult<Reservation>.Failure(EntityError.Conflict("Reservation could not be created."));
                    }
                }

                // Fallback path (no tenant, e.g. test environment without multi-tenancy)
                int seats = await CountActiveReservationsAsync(request.ShiftId, request.Date, cancellationToken);
                if (!request.CapacityOverride && seats >= ShiftBookingPolicyEvaluator.GetEffectiveCapacity(shift!, request.Date, policies))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return EntityResult<Reservation>.Failure(EntityError.Conflict("No seats are available for this shift slot."));
                }

                Reservation fallbackReservation = BuildReservation(shift!, request, policies);
                _ = dbContext.Reservations.Add(fallbackReservation);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return EntityResult<Reservation>.Success(fallbackReservation);
            });

        if (createResult.IsFailure)
        {
            return EntityResult<Guid>.Failure([.. createResult.Errors]);
        }

        if (createResult.Value is not Reservation created)
        {
            return EntityResult<Guid>.Failure(EntityError.Unknown("Reservation was not returned after creation."));
        }

        await eventPublisher.PublishReservationCreatedAsync(
            new ReservationCreatedEvent(created.Id, created.ShiftId, created.Date, created.ReservingPartyId),
            cancellationToken);

        await publisher.Publish(
            new ShiftReminderPolicyTriggeredEvent(
                created.Id,
                created.ShiftId,
                created.Date,
                created.ReservingPartyId,
                ReservationStatus.Pending),
            cancellationToken);

        return EntityResult<Guid>.Success(created.Id);
    }

    private static EntityResult ValidateShift(Shift? shift, DateOnly date, IReadOnlyList<ShiftBookingPolicy> policies)
    {
        if (shift is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Shift not found.", "shiftId"));
        }

        return shift.Status != ShiftStatus.Active
            ? EntityResult.Failure(EntityError.Conflict("This shift is not open for reservations."))
            : date.DayOfWeek != shift.DayOfWeek
            ? EntityResult.Failure(EntityError.Validation("Reservation date must match the shift day of week.", new Dictionary<string, string>
            {
                ["date"] = "Reservation date must match the shift day of week."
            }))
            : policies.Any(p => p.Date == date && p.Closed)
            ? EntityResult.Failure(EntityError.Conflict("This date is closed for reservations."))
            : EntityResult.Success();
    }

    private async Task<bool> HasCapacityForPartyAsync(
        Shift shift,
        DateOnly date,
        string? reservingPartyId,
        IReadOnlyList<ShiftBookingPolicy> policies,
        CancellationToken cancellationToken)
    {
        if (!ShiftBookingPolicyEvaluator.NeedsPartyCapacityCheck(shift, date, reservingPartyId, policies))
        {
            return true;
        }

        int partyCount = await dbContext.Reservations.CountAsync(r =>
            r.ShiftId == shift.Id &&
            r.Date == date &&
            r.ReservingPartyId == reservingPartyId &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Expired &&
            r.Status != ReservationStatus.Rejected,
            cancellationToken);

        return ShiftBookingPolicyEvaluator.HasCapacityForParty(shift, date, reservingPartyId, partyCount, policies);
    }

    private Reservation BuildReservation(Shift shift, CreateReservationCommand request, IReadOnlyList<ShiftBookingPolicy> policies) => new()
    {
        ShiftId = request.ShiftId,
        Date = request.Date,
        ReservingPartyId = request.ReservingPartyId,
        EstimatedSessionLength = ShiftBookingPolicyEvaluator.GetEffectiveEstimatedSessionLength(shift, request.Date, policies, request.EstimatedSessionLength),
        Status = ReservationStatus.Pending,
        IsHold = request.IsHold,
        ExpiresAt = request.IsHold && request.HoldDuration.HasValue
            ? DateTimeOffset.UtcNow.Add(request.HoldDuration.Value)
            : null
    };

    private Task<int> CountActiveReservationsAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken) =>
        dbContext.Reservations.CountAsync(r =>
            r.ShiftId == shiftId &&
            r.Date == date &&
            r.Status != ReservationStatus.Cancelled &&
            r.Status != ReservationStatus.Rejected &&
            r.Status != ReservationStatus.Expired &&
            !r.IsHold,
            cancellationToken);

    private async Task<IReadOnlyList<ShiftBookingPolicy>> LoadPoliciesAsync(Guid shiftId, CancellationToken cancellationToken)
        => await dbContext.ShiftBookingPolicies
            .AsNoTracking()
            .Where(p => p.ShiftId == shiftId)
            .ToListAsync(cancellationToken);
}
