using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vitarish.Modules.Base;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.DependencyInjection;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Sea.Interface.DependencyInjection;
using Vitarish.Sea.Presentation.Serializers;

namespace Vitarish.Sea.Interface;

/// <summary>
/// Module wiring for the Sea stack using the shared module contracts.
/// </summary>
public class SeaModule : ModuleBase, ISeaModule
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
        _ = services.AddSingleton<ISeaModule, SeaModule>();
        _ = services.AddMemoryCache();

        _ = services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Transient);

        _ = services.AddSeaInfrastructure();
        services.AddVitarishSeaDomainEntityHandlers();
        _ = services.AddAppMediatR(typeof(SeaDbContext).Assembly);

        _ = services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));

        _ = services.ConfigureSeaInterface();
    }

    protected override async Task InitializeAsync(WebApplication app) => await app.InitializeViews();
}
