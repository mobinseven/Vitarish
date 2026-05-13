namespace Vitarish.Appointments.Infrastructure.Repositories;

internal interface IShiftReservationViewRepository
{
    Task<IReadOnlyList<ShiftReservation>> GetAsync(
        IReadOnlyList<Guid> shiftIds, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken);
}

[RegisterTransient]
internal sealed class ShiftReservationViewRepository(AppointmentsDbContext dbContext) : IShiftReservationViewRepository
{
    public async Task<IReadOnlyList<ShiftReservation>> GetAsync(
        IReadOnlyList<Guid> shiftIds, DateOnly dateFrom, DateOnly dateTo, CancellationToken cancellationToken)
        => await dbContext.Set<ShiftReservation>()
            .AsNoTracking()
            .Where(sr => shiftIds.Contains(sr.ShiftId) && sr.Date >= dateFrom && sr.Date <= dateTo)
            .ToListAsync(cancellationToken);
}
