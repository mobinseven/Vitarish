using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Intelligence.Interface;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : ModuleBase, IIntelligenceModule
{
    /// <inheritdoc />
    public override string Name => "Intelligence";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        _ = services.AddSingleton<IIntelligenceModule, IntelligenceModule>();
        _ = services.AddSingleton<ModelRegistry>();
        _ = services.AddTransient<IResponder, ProviderDispatchingResponder>();
        _ = services.AddSingleton<ITranscriber, ProviderDispatchingTranscriber>();
        _ = services.AddAntiforgery();
        _ = services.ConfigureIntelligenceInterface();
    }

}
