namespace Vitarish.Appointments.Application.Abstractions;

public interface IShiftBookableDateQueryService
{
    Task<IReadOnlyList<ShiftReservationReadDto>> GetBookableDatesAsync(Guid shiftId, ShiftDateRangeQuery range, CancellationToken cancellationToken);

    Task<IReadOnlyList<BookableShiftSlot>> GetBookableSlotsAsync(ShiftDescriptorDto descriptor, ShiftDateRangeQuery range, CancellationToken cancellationToken);
}
