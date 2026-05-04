using Vitarish.Calendar.Infrastructure.Repositories;

namespace Vitarish.Calendar.Infrastructure.Services;

/// <summary>
/// Manages calendar accounts and their Radicale collections. Called programmatically.
/// </summary>
[RegisterTransient]
internal class CalendarAdminService(
    IEntityRepository<CalendarAccount, CalendarAccountDescriptor> accountRepository,
    CalendarSyncLedgerRepository ledgerRepository,
    IRadicaleAdapter radicaleAdapter,
    ICalendarSyncService syncService)
    : ICalendarAdminService
{
    public async Task CreateCalendarAccountAsync(CalendarAccountWriteDto dto, CancellationToken ct)
    {
        CalendarAccount account = dto.ToDomain();
        CalendarAccount? saved = await accountRepository.AddAsync(account, ct);

        if (saved is not null && !string.IsNullOrEmpty(saved.RadicaleCollectionUrl))
        {
            await radicaleAdapter.CreateCollectionAsync(
                saved.RadicaleCollectionUrl,
                saved.Id.ToString("N"),
                $"{saved.LocalEntityType}:{saved.LocalEntityId}",
                saved.DefaultTimeZone,
                ct);
        }
    }

    public async Task UpdateCalendarAccountAsync(CalendarAccountWriteDto dto, CancellationToken ct)
    {
        CalendarAccount? existing = await accountRepository.GetAsync(dto.Id, ct);
        if (existing is null)
        {
            return;
        }

        existing.LocalEntityId = dto.LocalEntityId;
        existing.LocalEntityType = dto.LocalEntityType;
        existing.DefaultTimeZone = dto.DefaultTimeZone;
        existing.IsActive = dto.IsActive;
        existing.RadicaleCollectionUrl = dto.RadicaleCollectionUrl;

        await accountRepository.UpdateAsync(existing, ct);
    }

    public async Task DeleteCalendarAccountAsync(Guid id, CancellationToken ct)
    {
        CalendarAccount? account = await accountRepository.GetAsync(id, ct);
        if (account is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(account.RadicaleCollectionUrl))
        {
            await radicaleAdapter.DeleteCollectionAsync(account.RadicaleCollectionUrl, ct);
        }

        await accountRepository.RemoveAsync(account, ct);
    }

    public async Task<IReadOnlyList<CalendarSyncLedgerReadDto>> GetSyncHealthAsync(CancellationToken ct)
    {
        IReadOnlyList<CalendarSyncLedger> ledgers = await ledgerRepository.GetAsync(
            new CalendarSyncLedgerDescriptor(), ct);

        return [.. ledgers.Select(l => l.ToDto())];
    }

    public async Task TriggerRescanAsync(Guid accountId, CancellationToken ct) =>
        await syncService.RescanAndRebuildAsync(accountId, ct);
}
