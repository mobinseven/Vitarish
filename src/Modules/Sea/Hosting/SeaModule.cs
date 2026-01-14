using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.DependencyInjection;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Sea.Interface.DependencyInjection;
using Vitarish.Sea.Presentation.Serializers;

namespace Vitarish.Sea.Hosting;

/// <summary>
/// Module wiring for the Sea stack using the shared module contracts.
/// </summary>
public class SeaModule : ModuleBase
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaModule"/> class.
    /// </summary>
    public SeaModule()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaModule"/> class with database configuration.
    /// </summary>
    /// <param name="configureDbContext">Delegate to configure the database context.</param>
    public SeaModule(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    /// <inheritdoc />
    public override string Name => "Sea";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = (configureDbContext ?? this.configureDbContext) ?? throw new InvalidOperationException("SeaModule requires a DbContext configuration delegate.");

        _ = configuration;
        _ = services.AddMemoryCache();

        _ = services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Transient);

        _ = services.AddSeaInfrastructure();
        services.AddVitarishSeaDomainEntityHandlers();
        _ = services.AddAppMediatR(typeof(SeaDbContext).Assembly);

        _ = services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));

        services.ConfigureSeaInterface();
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
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
        _ = endpoints.MapSeaEndpoints();
    }

    protected override async Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints)
    {
        if (endpoints is not WebApplication app)
        {
            throw new InvalidOperationException("SeaModule requires a WebApplication for endpoint configuration.");
        }

        await app.InitializeViews();
    }

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        builder.Services.AddHealthChecks();
}
