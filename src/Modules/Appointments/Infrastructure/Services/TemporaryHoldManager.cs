using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Infrastructure.Services;

[RegisterTransient]
internal class TemporaryHoldManager(
    IEntityRepository<Reservation, ReservationDescriptor> reservationRepository,
    AppointmentsDbContext dbContext) : ITemporaryHoldManager
{
    public async Task<EntityResult<Guid>> CreateHoldAsync(
        Guid shiftId, DateOnly date, string? reservingPartyId, TimeSpan duration, CancellationToken cancellationToken)
    {
        Reservation hold = new()
        {
            ShiftId = shiftId,
            Date = date,
            ReservingPartyId = reservingPartyId,
            Status = ReservationStatus.Pending,
            IsHold = true,
            ExpiresAt = DateTime.UtcNow.Add(duration)
        };

        Reservation created = await reservationRepository.AddAsync(hold, cancellationToken);
        return EntityResult<Guid>.Success(created.Id);
    }

    public async Task<EntityResult> ConfirmHoldAsync(Guid holdId, CancellationToken cancellationToken)
    {
        Reservation? hold = await reservationRepository.GetAsync(holdId, cancellationToken);

        if (hold is null || !hold.IsHold)
        {
            return EntityResult.Failure(EntityError.NotFound("Hold not found.", nameof(holdId)));
        }

        if (hold.ExpiresAt.HasValue && hold.ExpiresAt.Value < DateTime.UtcNow)
        {
            return EntityResult.Failure(EntityError.Conflict("Hold has expired."));
        }

        hold.IsHold = false;
        hold.ExpiresAt = null;
        hold.Status = ReservationStatus.Pending;

        _ = await reservationRepository.UpdateAsync(hold, cancellationToken);
        return EntityResult.Success();
    }

    public async Task<EntityResult> ReleaseHoldAsync(Guid holdId, CancellationToken cancellationToken)
    {
        Reservation? hold = await reservationRepository.GetAsync(holdId, cancellationToken);

        if (hold is null || !hold.IsHold)
        {
            return EntityResult.Failure(EntityError.NotFound("Hold not found.", nameof(holdId)));
        }

        hold.Status = ReservationStatus.Cancelled;
        hold.CancelledAt = DateTime.UtcNow;

        _ = await reservationRepository.UpdateAsync(hold, cancellationToken);
        return EntityResult.Success();
    }

    public async Task ExpireStaleHoldsAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        List<Reservation> staleHolds = await dbContext.Reservations
            .Where(r => r.IsHold && r.ExpiresAt.HasValue && r.ExpiresAt.Value < now)
            .ToListAsync(cancellationToken);

        foreach (Reservation hold in staleHolds)
        {
            hold.Status = ReservationStatus.Expired;
            hold.IsHold = false;
        }

        if (staleHolds.Count > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
