using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Infrastructure.DependencyInjection;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;
using Vitarish.Intelligence.Infrastructure.Services.Google;

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

        _ = services.AddOptions<LiveTranscriptionSettings>()
            .BindConfiguration($"{sectionPath}:LiveTranscription")
            .Validate(static settings =>
            {
                try
                {
                    settings.Validate();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }, "Live transcription limits, languages, and MIME types must form a valid bounded configuration.")
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
        _ = builder.Services.AddSingleton(sp =>
            sp.GetRequiredService<IOptions<LiveTranscriptionSettings>>().Value);
        builder.Services.TryAddSingleton(TimeProvider.System);
        _ = builder.Services.AddSingleton<ITranscriptionProvider, GoogleLiveTranscriptionProvider>();
        _ = builder.Services.AddSingleton<LiveTranscriptionCapabilitiesQuery>();
        _ = builder.Services.AddSingleton<LiveTranscriptionSessionFactory>();
        _ = builder.Services.AddSingleton(new RealtimeTranscriptionOriginPolicy(
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? []));
        _ = builder.Services.AddAntiforgery();
        _ = builder.Services.AddIntelligenceBackgroundJobs();
    }

}
