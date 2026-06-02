using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Hosting.Endpoints;
using Vitarish.Social.Infrastructure.Data;
using Vitarish.Social.Infrastructure.DependencyInjection;

namespace Vitarish.Social.Hosting;

public sealed class SocialModule : Module, ISocialModule
{
    public override string Name => "Social";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ISocialModule, SocialModule>();

        _ = builder.Services.AddDbContext<SocialDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);

        _ = builder.Services.AddSocialInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        bool enabled = app.Configuration.GetValue<bool>("Vitarish:Features:SocialIntegrations");
        if (!enabled)
        {
            return Task.CompletedTask;
        }

        _ = app.MapSocialWebhookEndpoints();
        return Task.CompletedTask;
    }
}
