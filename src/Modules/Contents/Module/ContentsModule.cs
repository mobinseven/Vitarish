using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Agents;
using Vitarish.Contents.Application;
using Vitarish.Contents.Api;
using Vitarish.Contents.Infrastructure.Data;
using Vitarish.Contents.Infrastructure.DependencyInjection;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Vitarish.Contents.Contracts.Routes;

namespace Vitarish.Contents.Module;

public sealed class ContentsModule : Modules.Base.Core.Module, IContentsModule
{
    public override string Name => "Contents";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishContentsDomainEntityHandlers();

        _ = builder.Services.AddSingleton<IContentsModule, ContentsModule>();
        _ = builder.Services.AddScoped<IContentManagementService, ContentManagementService>();
        _ = builder.Services.AddScoped<IContentAuthoring, ContentAuthoring>();
        _ = builder.Services.AddScoped<IContentSeeding, ContentSeeding>();
        _ = builder.Services.AddScoped<ContentAuthoringQueries>();
        _ = builder.Services.AddScoped<IContentAuthoringQueries>(services => services.GetRequiredService<ContentAuthoringQueries>());
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsListSitesAgentTool>();
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsListTypesAgentTool>();
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsListEntriesAgentTool>();
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsGetEntryAgentTool>();
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsCreateDraftEntryAgentTool>();
        _ = builder.Services.AddScoped<IContentsAgentTool, ContentsSaveDraftAgentTool>();
        _ = builder.Services.AddScoped<ContentDelivery>();
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, ContentsJsonSerializerContext.Default));
        _ = builder.Services.AddRateLimiter(options => options.AddPolicy(
            ContentsPublicDelivery.RateLimitPolicy,
            context => RateLimitPartition.GetSlidingWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 60,
                    SegmentsPerWindow = 6,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                })));

        _ = builder.Services.AddTransientMultiTenantDbContext<ContentsDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddContentsInfrastructure();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        await app.MigrateDatabaseAsync<ContentsDbContext>();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapContentsApi();
}
