using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Vitarish.Commerce.Api;
using Vitarish.Commerce.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Commerce.Infrastructure.Data;
using Vitarish.Commerce.Infrastructure.DependencyInjection;
using Vitarish.Commerce.Infrastructure.Services;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Commerce.Module;

public class CommerceModule : Modules.Base.Core.Module, ICommerceModule
{
    public override string Name => "Commerce";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddSingleton<ICommerceModule, CommerceModule>();

        _ = builder.Services.AddTransientMultiTenantDbContext<CommerceDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddCommerceInfrastructure();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = app.MapCommerceApi();
        _ = await app.MigrateDatabaseAsync<CommerceDbContext>();
        if (!string.Equals(Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"), "true", StringComparison.OrdinalIgnoreCase))
            await app.InitializeViews();
    }
}
