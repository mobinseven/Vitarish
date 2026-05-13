namespace Vitarish.Appointments.Infrastructure.Services;

internal interface IShiftAggregateLoader
{
    Task<Shift?> LoadWithPoliciesAsync(Guid shiftId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
