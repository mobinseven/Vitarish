using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Hosting.Hubs;
using Vitarish.Intelligence.Infrastructure.Services;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Intelligence.Hosting;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : ModuleBase
{
    /// <inheritdoc />
    public override string Name => "Intelligence";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        _ = services.AddSingleton<ModelRegistry>();
        _ = services.AddTransient<IResponder, ProviderDispatchingResponder>();
        _ = services.AddSingleton<ITranscriber, ProviderDispatchingTranscriber>();
        _ = services.AddAntiforgery();
        services.ConfigureIntelligenceInterface();
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapHub<ResponseHub>("/ai/hubs/response");
        _ = endpoints.MapHub<SpeechHub>("/ai/hubs/speech");
    }

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        builder.Services.AddHealthChecks();

    /// <inheritdoc />
    protected override void ConfigureSelfTests(WebApplicationBuilder builder) => builder.Services.AddHostedService<IntelligenceStartupSelfTest>();
}
