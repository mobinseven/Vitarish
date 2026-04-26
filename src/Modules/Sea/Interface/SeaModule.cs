using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.DependencyInjection;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Sea.Interface.DependencyInjection;
using Vitarish.Sea.Presentation.Serializers;

namespace Vitarish.Sea.Interface;
/// <summary>
/// SEA is short for state-event-action, which is the concept behind this module which provides project management features.
/// </summary>
public class SeaModule : Module, ISeaModule
{
    public override string Name => "Sea";

    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishSeaDomainEntityHandlers();

        _ = builder.Services.AddSingleton<ISeaModule, SeaModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddDbContext<SeaDbContext>((_, options) => configureDbContext(options), ServiceLifetime.Transient);

        _ = builder.Services.AddSeaInfrastructure();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));

        _ = builder.Services.ConfigureInterface();
    }

    public override async Task Initialize(WebApplication app) => await app.InitializeViews();
}
