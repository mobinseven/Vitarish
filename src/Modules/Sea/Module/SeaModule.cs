using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Microsoft.EntityFrameworkCore;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.DependencyInjection;
using Vitarish.Sea.Infrastructure.Services;
using Vitarish.Sea.Infrastructure.Agents;
using Vitarish.Sea.Api;
using Vitarish.Sea.Api.Authorization;
using Vitarish.Sea.Presentation.Serializers;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Sea.Module;
/// <summary>
/// SEA is short for state-event-action, which is the concept behind this module which provides project management features.
/// </summary>
public class SeaModule : Modules.Base.Core.Module, ISeaModule
{
    public override string Name => "Sea";

    public override async Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishSeaDomainEntityHandlers();

        _ = builder.Services.AddSingleton<ISeaModule, SeaModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddTransientMultiTenantDbContext<SeaDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddSeaInfrastructure();
        _ = builder.Services.AddScoped<ISeaManagementAccessEvaluator, SeaManagementAccessEvaluator>();
        _ = builder.Services.AddTransient<ISeaManagementAgentTool, SeaManagementAgentTool>();
        _ = builder.Services.AddSeaApiAuthorization();

        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, SeaJsonSerializerContext.Default));

    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<SeaDbContext>();
        await app.InitializeViews();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapSeaApi();
}
