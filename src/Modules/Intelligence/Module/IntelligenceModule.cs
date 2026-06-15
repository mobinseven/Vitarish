using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Interface.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Intelligence.Module;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : Vitarish.Modules.Base.Module, IIntelligenceModule
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
        _ = builder.Services.AddAntiforgery();
        _ = builder.Services.ConfigureIntelligenceInterface();
    }

}
