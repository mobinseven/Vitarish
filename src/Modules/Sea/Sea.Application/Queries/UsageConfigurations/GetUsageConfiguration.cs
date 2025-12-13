using Vitarish.Sea.Domain.Entities;

namespace Vitarish.Sea.Application.Queries.UsageConfigurations;

public record GetUsageConfigurationQuery : IRequest<UsageConfiguration?>;

