namespace Vitarish.Calendar.Application.Abstractions;

/// <summary>
/// Collection and account management. Called programmatically by admin tooling or other modules.
/// Never exposed directly to end-users or admins through a UI.
/// </summary>
public interface ICalendarAdminService
{
    Task CreateCalendarAccountAsync(CalendarAccountWriteDto dto, CancellationToken ct);

    Task UpdateCalendarAccountAsync(CalendarAccountWriteDto dto, CancellationToken ct);

    Task DeleteCalendarAccountAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<CalendarSyncLedgerReadDto>> GetSyncHealthAsync(CancellationToken ct);

    Task TriggerRescanAsync(Guid accountId, CancellationToken ct);
}
