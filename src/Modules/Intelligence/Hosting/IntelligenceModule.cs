using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Hosting.Hubs;
using Vitarish.Intelligence.Infrastructure.Services;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Modules.Base;

namespace Vitarish.Intelligence.Hosting;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : ModuleBase
{
    /// <inheritdoc />
    public override string Name => IntelligenceModuleConfiguration.ModuleName;

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        IntelligenceModuleConfiguration.ConfigureCoreServices(services);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints) => IntelligenceModuleConfiguration.MapEndpoints(endpoints);

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) => IntelligenceModuleConfiguration.ConfigureHealthChecks(builder.Services);

    /// <inheritdoc />
    protected override void ConfigureSelfTests(WebApplicationBuilder builder) => IntelligenceModuleConfiguration.ConfigureSelfTests(builder.Services);
}

internal static class IntelligenceModuleConfiguration
{
    internal const string ModuleName = "Intelligence";

    internal static void ConfigureCoreServices(IServiceCollection services)
    {
        _ = services.AddSingleton<ModelRegistry>();
        _ = services.AddTransient<IResponder, ProviderDispatchingResponder>();
        _ = services.AddSingleton<ITranscriber, ProviderDispatchingTranscriber>();
        _ = services.AddAntiforgery();
        _ = services.AddSignalR().AddMessagePackProtocol();
    }

    internal static void ConfigureHealthChecks(IServiceCollection services) =>
        // Reserved for Intelligence-specific health checks (e.g., AI provider reachability).
        services.AddHealthChecks();

    internal static void ConfigureSelfTests(IServiceCollection services) => services.AddHostedService<IntelligenceStartupSelfTest>();

    internal static void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ResponseHub>("/ai/hubs/response");
        endpoints.MapHub<SpeechHub>("/ai/hubs/speech");
    }
}
