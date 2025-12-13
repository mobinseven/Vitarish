using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Sea.Infrastructure;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Presentation.Serializers;

namespace Vitarish.Sea.Hosting;

internal static class SeaModuleConfiguration
{
    public static void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        // Reserved for future Sea-specific configuration binding.
        _ = configuration;
        _ = services.AddMemoryCache();

        _ = services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Transient, ServiceLifetime.Transient);

        _ = services.AddVitarishSeaInfrastructure();
        _ = services.AddAppMediatR(typeof(SeaDbContext).Assembly);

        _ = services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    public static void ConfigureHealthChecks(IServiceCollection services) =>
        services.AddHealthChecks();
}
