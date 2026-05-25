using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Infrastructure.Services.Rag;
using Vitarish.Intelligence.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Intelligence.Interface;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : Module, IIntelligenceModule
{
    /// <inheritdoc />
    public override string Name => "Intelligence";

    public override Task Initialize(WebApplication app) => Task.CompletedTask;

    /// <inheritdoc />
    public override async Task Register(WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        _ = builder.Services.AddSingleton<IIntelligenceModule, IntelligenceModule>();
        _ = builder.Services.AddSingleton<ModelRegistry>();
        _ = builder.Services.AddTransient<IResponder, ProviderDispatchingResponder>();
        _ = builder.Services.AddSingleton<ITranscriber, ProviderDispatchingTranscriber>();
        builder.Services.TryAddSingleton<IRagSearchBridge, NullRagSearchBridge>();
        _ = builder.Services.AddScoped<IRagRetrievalService, RetrieveOnlyRagService>();
        _ = builder.Services.AddScoped<IRagHealthCheckService, RagHealthCheckService>();
        _ = builder.Services.AddSingleton<IRagCapabilityCatalog, RagCapabilityCatalog>();
        _ = builder.Services.AddScoped<IRagGroundedGenerationService, PlaceholderRagGroundedGenerationService>();
        _ = builder.Services.AddScoped<IRagRerankingService, PlaceholderRagRerankingService>();
        _ = builder.Services.AddScoped<IRagPlatformAdminService, PlaceholderRagPlatformAdminService>();
        _ = builder.Services.AddAntiforgery();
        _ = builder.Services.ConfigureIntelligenceInterface();
    }

}
