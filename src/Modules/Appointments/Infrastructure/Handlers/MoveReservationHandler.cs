using Vitarish.Appointments.Application.Events;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class MoveReservationHandler(
    AppointmentsDbContext dbContext,
    ShiftBookingPolicyEvaluator policyEvaluator,
    IAppointmentsEventPublisher eventPublisher,
    IRequestContext requestContext)
    : IRequestHandler<MoveReservationCommand>
{
    public async Task Handle(MoveReservationCommand request, CancellationToken cancellationToken)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            Reservation? original = await dbContext.Reservations
                .FirstOrDefaultAsync(r => r.Id == request.ReservationId, cancellationToken);

            if (original is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Shift? targetShift = await dbContext.Shifts
                .AsNoTracking()
                .Include(s => s.BookingPolicies)
                .FirstOrDefaultAsync(s => s.Id == request.TargetShiftId, cancellationToken);

            if (targetShift is null || targetShift.Status != ShiftStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            if (!policyEvaluator.IsBookingOpen(targetShift, request.TargetDate))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Guid fromShiftId = original.ShiftId;
            DateOnly fromDate = original.Date;

            original.Status = ReservationStatus.Cancelled;
            original.CancelledAt = DateTimeOffset.UtcNow;

            string? tenantId = requestContext.TenantId;
            ShiftReservationLock? lockEntry = tenantId is not null
                ? new() { ShiftId = request.TargetShiftId, Date = request.TargetDate, TenantId = tenantId }
                : null;

            if (lockEntry is not null)
            {
                dbContext.ShiftReservationLocks.Add(lockEntry);
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException lockEx) when (AppointmentsDatabaseExceptions.IsUniqueConstraintViolation(lockEx))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            int seats = await dbContext.Reservations.CountAsync(r =>
                r.ShiftId == request.TargetShiftId &&
                r.Date == request.TargetDate &&
                r.Status != ReservationStatus.Cancelled &&
                r.Status != ReservationStatus.Rejected &&
                r.Status != ReservationStatus.Expired &&
                !r.IsHold,
                cancellationToken);

            if (seats >= policyEvaluator.GetEffectiveCapacity(targetShift, request.TargetDate))
            {
                if (lockEntry is not null)
                {
                    dbContext.ShiftReservationLocks.Remove(lockEntry);
                }
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Reservation newReservation = new()
            {
                ShiftId = request.TargetShiftId,
                Date = request.TargetDate,
                ReservingPartyId = original.ReservingPartyId,
                Status = ReservationStatus.Pending
            };

            dbContext.Reservations.Add(newReservation);

            if (lockEntry is not null)
            {
                dbContext.ShiftReservationLocks.Remove(lockEntry);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await eventPublisher.PublishReservationMovedAsync(
                new ReservationMovedEvent(
                    original.Id,
                    fromShiftId,
                    fromDate,
                    newReservation.Id,
                    request.TargetShiftId,
                    request.TargetDate,
                    original.ReservingPartyId),
                cancellationToken);
        });
    }
}
