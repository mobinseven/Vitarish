namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class ReceiptRepository(SeaDbContext dbContext)
    : EntityRepository<Receipt, ReceiptDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Receipt, ReceiptDescriptor>
{
    protected override IQueryable<Receipt> Apply(IQueryable<Receipt> query, ReceiptDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ProjectId is not null)
        {
            Guid projectId = descriptor.ProjectId.Value;
            query = query.Where(r =>
                dbContext.Procedures.Any(p => p.Id == r.ObjectId && p.ProjectId == projectId));
        }

        if (descriptor.ProcedureId is not null)
        {
            Guid procedureId = descriptor.ProcedureId.Value;
            query = query.Where(r =>
                dbContext.Procedures.Any(p => p.Id == r.ObjectId && p.Id == procedureId));
        }

        if (descriptor.EventId is not null)
        {
            Guid eventId = descriptor.EventId.Value;
            query = query.Where(r =>
                dbContext.Events.Any(e => e.Id == r.ObjectId && e.Id == eventId));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.CreatedBy))
        {
            query = query.Where(r => r.CreatedBy == descriptor.CreatedBy);
        }

        if (descriptor.ObjectId is not null)
        {
            query = query.Where(r => r.ObjectId == descriptor.ObjectId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}

