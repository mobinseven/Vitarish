using Microsoft.Extensions.DependencyInjection;
using Vitarish.Journey.Infrastructure.DependencyInjection;

namespace Vitarish.Journey.Module.DependencyInjection;

public static class JourneyModuleServiceCollectionExtensions
{
    public static IServiceCollection AddJourneyModuleServices(this IServiceCollection services)
    {
        return services.AddJourney();
    }
}
