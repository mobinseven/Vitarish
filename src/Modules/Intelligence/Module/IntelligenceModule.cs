using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Infrastructure.DependencyInjection;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Interface.DependencyInjection;

namespace Vitarish.Intelligence.Module;

/// <summary>
/// Module wiring for the Intelligence stack using the shared module contracts.
/// </summary>
public class IntelligenceModule : Modules.Base.Core.Module, IIntelligenceModule
{
    /// <inheritdoc />
    public override string Name => "Intelligence";

    public override Task Initialize(WebApplication app)
    {
        _ = app.MapIntelligenceApi();
        return Task.CompletedTask;
    }

    public override void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath)
    {
        _ = services.AddOptions<AiOptions>()
            .BindConfiguration(sectionPath)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <inheritdoc />
    public override async Task Register(WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        RegisterOptions(builder.Services, builder.Configuration, OptionsSectionPath);
        _ = builder.Services.AddSingleton<IIntelligenceModule, IntelligenceModule>();
        _ = builder.Services.AddSingleton<ModelRegistry>();
        _ = builder.Services.AddTransient<IResponder, ProviderDispatchingResponder>();
        _ = builder.Services.AddSingleton<ITranscriber, ProviderDispatchingTranscriber>();
        _ = builder.Services.AddAntiforgery();
        _ = builder.Services.ConfigureIntelligenceInterface();
        _ = builder.Services.AddIntelligenceBackgroundJobs();
    }

}
