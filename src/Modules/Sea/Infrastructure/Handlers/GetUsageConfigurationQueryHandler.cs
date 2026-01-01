using Vitarish.Sea.Application.Queries.UsageConfigurations;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class GetUsageConfigurationQueryHandler(IEntityRepository<UsageConfiguration, UsageConfigurationDescriptor> repository)
    : IRequestHandler<GetUsageConfigurationQuery, UsageConfiguration?>
{
    public async Task<UsageConfiguration?> Handle(GetUsageConfigurationQuery request, CancellationToken cancellationToken) => (await repository.GetAsync(new UsageConfigurationDescriptor(), cancellationToken)).FirstOrDefault();
}

