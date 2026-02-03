using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vitarish.Cms.Application.Abstractions;
using Vitarish.Cms.Infrastructure.Data;
using Vitarish.Cms.Infrastructure.DependencyInjection;
using Vitarish.Cms.Interface.DependencyInjection;
using Vitarish.Estates.Base;

namespace Vitarish.Cms.Interface;

public class CmsEstate : Estate, ICmsEstate
{
    private readonly Action<DbContextOptionsBuilder>? configureDbContext;

    public CmsEstate()
    {
    }

    public CmsEstate(Action<DbContextOptionsBuilder> configureDbContext)
    {
        this.configureDbContext = configureDbContext;
    }

    public override string Name => "Cms";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        ConfigureServices(builder.Services, builder.Configuration, configureDbContext);
        return Task.CompletedTask;
    }

    protected void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        Action<DbContextOptionsBuilder>? dbConfigurator = (configureDbContext ?? this.configureDbContext) ?? throw new InvalidOperationException("CmsEstate requires a DbContext configuration delegate.");

        _ = configuration;
        _ = services.AddSingleton<ICmsEstate, CmsEstate>();
        _ = services.AddMemoryCache();

        _ = services.AddDbContext<CmsDbContext>((_, options) => dbConfigurator(options), ServiceLifetime.Transient);

        _ = services.AddCmsInfrastructure();
        _ = services.ConfigureCmsInterface();
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
