using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Appointments.Application.Events;
using Vitarish.Appointments.Infrastructure.Services;
using Vitarish.Entities.Server.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Appointments.Infrastructure.Handlers;

internal class MoveReservationHandler(
    AppointmentsDbContext dbContext,
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
                .FirstOrDefaultAsync(s => s.Id == request.TargetShiftId, cancellationToken);

            if (targetShift is null || targetShift.Status != ShiftStatus.Active)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            IReadOnlyList<ShiftBookingPolicy> targetPolicies = await dbContext.ShiftBookingPolicies
                .AsNoTracking()
                .Where(p => p.ShiftId == request.TargetShiftId)
                .ToListAsync(cancellationToken);

            if (!ShiftBookingPolicyEvaluator.IsBookingOpen(targetShift, request.TargetDate, targetPolicies))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            Guid fromShiftId = original.ShiftId;
            DateOnly fromDate = original.Date;

            original.Status = ReservationStatus.Cancelled;
            original.CancelledAt = DateTime.UtcNow;

            string? tenantId = requestContext.TenantId;
            ShiftReservationLock? lockEntry = tenantId is not null
                ? new() { ShiftId = request.TargetShiftId, Date = request.TargetDate, TenantId = tenantId }
                : null;

            if (lockEntry is not null)
            {
                _ = dbContext.ShiftReservationLocks.Add(lockEntry);
            }

            try
            {
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException lockEx) when (DatabaseExceptions.IsUniqueConstraintViolation(lockEx))
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

            if (seats >= ShiftBookingPolicyEvaluator.GetEffectiveCapacity(targetShift, request.TargetDate, targetPolicies))
            {
                if (lockEntry is not null)
                {
                    _ = dbContext.ShiftReservationLocks.Remove(lockEntry);
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

            _ = dbContext.Reservations.Add(newReservation);

            if (lockEntry is not null)
            {
                _ = dbContext.ShiftReservationLocks.Remove(lockEntry);
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
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
