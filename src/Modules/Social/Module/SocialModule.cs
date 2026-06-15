using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Module.Endpoints;
using Vitarish.Social.Infrastructure.Data;
using Vitarish.Social.Infrastructure.DependencyInjection;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Social.Module;

public sealed class SocialModule : Vitarish.Modules.Base.Module, ISocialModule
{
    public override string Name => "Social";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ISocialModule, SocialModule>();

        _ = builder.Services.AddTransientMultiTenantDbContext<SocialDbContext>((_, options) =>
        {
            configureDbContext?.Invoke(options);
            options.UseNpgsql(
                builder.Configuration.GetSection("ConnectionStrings")["Db"],
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(SocialDbContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure();
                });
        });

        _ = builder.Services.AddSocialInfrastructure();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<SocialDbContext>();
        bool enabled = app.Configuration.GetValue<bool>("Vitarish:Features:SocialIntegrations");
        if (!enabled)
        {
            return;
        }

        _ = app.MapSocialWebhookEndpoints();
    }
}
