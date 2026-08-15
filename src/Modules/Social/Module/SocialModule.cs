using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Social.Api;
using Vitarish.Social.Application.Abstractions;
using Vitarish.Social.Module.Endpoints;
using Vitarish.Social.Infrastructure.Data;
using Vitarish.Social.Infrastructure.DependencyInjection;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Social.Module;

public sealed class SocialModule : Modules.Base.Core.Module, ISocialModule
{
    public override string Name => "Social";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ISocialModule, SocialModule>();

        _ = builder.Services.AddTransientMultiTenantDbContext<SocialDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddSocialInfrastructure();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<SocialDbContext>();
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapSocialApi();
        if (endpoints.ServiceProvider.GetRequiredService<IConfiguration>().GetValue<bool>("Vitarish:Features:SocialIntegrations"))
            _ = endpoints.MapSocialWebhookEndpoints();
    }
}
