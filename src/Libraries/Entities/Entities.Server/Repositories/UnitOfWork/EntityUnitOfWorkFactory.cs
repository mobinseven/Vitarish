using Microsoft.EntityFrameworkCore.Storage;

namespace Vitarish.Entities.Server.Repositories.UnitOfWork;

internal sealed class EntityUnitOfWorkFactory<TDbContext>(IDbContextFactory<TDbContext> dbContextFactory)
    : IEntityUnitOfWorkFactory<TDbContext>
    where TDbContext : DbContext
{
    public async Task<IEntityUnitOfWork<TDbContext>> BeginAsync(CancellationToken cancellationToken = default)
    {
        TDbContext context = dbContextFactory.CreateDbContext();
        IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EntityUnitOfWork<TDbContext>(context, transaction);
    }
}
