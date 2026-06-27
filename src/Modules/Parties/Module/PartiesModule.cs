using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Aggregator.Core;
using Vitarish.Parties.Module.DependencyInjection;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Api;
using Vitarish.Parties.Interface.DependencyInjection;

namespace Vitarish.Parties.Module;

/// <summary>
/// Module composition root for Parties registration.
/// </summary>
public sealed class PartiesModule : Vitarish.Modules.Aggregator.Core.Module
{
    public override string Name => "Parties";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddParties(configureDbContext ?? (_ => { }));
        _ = builder.Services.ConfigurePartiesInterface();

        return Task.CompletedTask;
    }

    public override async Task Initialize(WebApplication app)
    {
        _ = await app.MigrateDatabaseAsync<PartiesDbContext>();
        await app.InitializeViews();
        _ = app.MapPartiesApi();
    }
}
