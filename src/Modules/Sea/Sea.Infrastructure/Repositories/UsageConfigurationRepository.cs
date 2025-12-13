using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]

internal class UsageConfigurationRepository(SeaDbContext dbContext) : EntityRepository<UsageConfiguration, UsageConfigurationDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<UsageConfiguration, UsageConfigurationDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}

