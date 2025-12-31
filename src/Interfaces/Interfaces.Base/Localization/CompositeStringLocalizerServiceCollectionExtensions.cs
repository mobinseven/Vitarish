using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Interface.Localization;

namespace Vitarish.Interfaces.Base.Localization;

public static class CompositeStringLocalizerServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureCompositeStringLocalizer(params Type[] resourceTypesInPriorityOrder)
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
            _ = services.AddSingleton<MudLocalizer, CustomMudLocalizer>();
            return services;
        }
    }
}
