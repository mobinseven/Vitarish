using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vitarish.Estates.Base;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.DependencyInjection;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Sea.Interface.DependencyInjection;
using Vitarish.Sea.Presentation.Serializers;

namespace Vitarish.Sea.Interface;

public class SeaEstate : Estate, ISeaEstate
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaEstate"/> class.
    /// </summary>
    public SeaEstate()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SeaEstate"/> class with database configuration.
    /// </summary>
    /// <param name="configureDbContext">Delegate to configure the database context.</param>
    public SeaEstate(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    /// <inheritdoc />
    public override string Name => "Sea";

    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null) => ConfigureServices(builder.Services, builder.Configuration, configureDbContext);

    /// <inheritdoc />
    protected void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = (configureDbContext ?? this.configureDbContext) ?? throw new InvalidOperationException("SeaEstate requires a DbContext configuration delegate.");

        _ = configuration;
        _ = services.AddSingleton<ISeaEstate, SeaEstate>();
        _ = services.AddMemoryCache();

        _ = services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Transient);

        _ = services.AddSeaInfrastructure();
        services.AddVitarishSeaDomainEntityHandlers();
        _ = services.AddAppMediatR(typeof(SeaDbContext).Assembly);

        _ = services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));

        _ = services.ConfigureSeaInterface();
    }

    public override async Task Initialize(WebApplication app) => await app.InitializeViews();
}
