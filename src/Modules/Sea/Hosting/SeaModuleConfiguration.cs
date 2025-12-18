using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        _ = services.AddSignalR().AddMessagePackProtocol();

        _ = services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Scoped);

        _ = services.AddVitarishSeaInfrastructure();
        services.AddVitarishSeaDomainEntityHandlers();
        _ = services.AddAppMediatR(typeof(SeaDbContext).Assembly);

        _ = services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));
    }

    public static void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("SeaModule requires a WebApplication for endpoint configuration.");
        }

        _ = app.MapHub<Hubs.ProjectsHub>("/sea/hubs/projects");
        _ = app.MapHub<Hubs.ProceduresHub>("/sea/hubs/procedures");
        _ = app.MapHub<Hubs.EventsHub>("/sea/hubs/events");
        _ = app.MapHub<Hubs.AttachmentsHub>("/sea/hubs/attachments");
        _ = app.MapHub<Hubs.EventRolesHub>("/sea/hubs/event-roles");
        _ = app.MapHub<Hubs.RolesHub>("/sea/hubs/roles");
        _ = app.MapHub<Hubs.UserRolesHub>("/sea/hubs/user-roles");
        _ = app.MapHub<Hubs.ReceiptsHub>("/sea/hubs/receipts");
        _ = app.MapHub<Hubs.RemindersHub>("/sea/hubs/reminders");
        _ = app.MapHub<Hubs.UsageConfigurationHub>("/sea/hubs/usage-configuration");
        endpoints.MapSeaEndpoints();
    }

    public static void ConfigureHealthChecks(IServiceCollection services) =>
        services.AddHealthChecks();
}
