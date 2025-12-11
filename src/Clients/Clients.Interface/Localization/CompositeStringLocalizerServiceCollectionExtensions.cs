using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Vitarish.Clients.Interface.Localization;

public static class CompositeStringLocalizerServiceCollectionExtensions
{
    public static IServiceCollection ConfigureCompositeStringLocalizer(
        this IServiceCollection services,
        params Type[] resourceTypesInPriorityOrder)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(resourceTypesInPriorityOrder);

        _ = services.Configure<CompositeStringLocalizerOptions>(options
            => options.ResourceTypesInPriorityOrder = [.. resourceTypesInPriorityOrder
                .Concat(options.ResourceTypesInPriorityOrder)
                .Distinct()]);

        services.TryAddSingleton<ResourceManagerStringLocalizerFactory>(sp
            => ActivatorUtilities.CreateInstance<ResourceManagerStringLocalizerFactory>(sp));

        _ = services.AddSingleton<IStringLocalizerFactory>(sp
            => new CompositeStringLocalizerFactory(
                sp.GetRequiredService<ResourceManagerStringLocalizerFactory>(),
                sp.GetRequiredService<IOptions<CompositeStringLocalizerOptions>>()));

        return services;
    }
}
