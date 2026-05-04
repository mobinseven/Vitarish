using Vitarish.Calendar.Domain.Models;

namespace Vitarish.Calendar.Infrastructure.Repositories;

/// <summary>
/// Append-only audit repository. Only AddAsync is exposed to consumers.
/// Inherits full EntityRepository but callers should use only AddAsync.
/// </summary>
[RegisterTransient]
internal class CalendarAuditRepository(CalendarDbContext dbContext)
    : EntityRepository<CalendarAuditEntry, CalendarAuditEntryDescriptor, CalendarDbContext>(dbContext),
      IEntityRepository<CalendarAuditEntry, CalendarAuditEntryDescriptor>
{
    protected override IQueryable<CalendarAuditEntry> Apply(IQueryable<CalendarAuditEntry> query, CalendarAuditEntryDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ObjectMappingId is { } mappingId)
        {
            query = query.Where(e => e.ObjectMappingId == mappingId);
        }

        if (descriptor.AccountId is { } accountId)
        {
            query = query.Where(e => e.AccountId == accountId);
        }

        if (descriptor.Action is { } action)
        {
            query = query.Where(e => e.Action == action);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
