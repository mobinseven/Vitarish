namespace Vitarish.Appointments.Infrastructure.Services;

internal interface ISessionAggregateLoader
{
    Task<Session?> LoadWithPoliciesAsync(Guid sessionId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
