using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterScoped]

internal class UsageConfigurationRepository(SeaDbContext dbContext) : EntityRepository<UsageConfiguration, UsageConfigurationDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<UsageConfiguration, UsageConfigurationDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}

