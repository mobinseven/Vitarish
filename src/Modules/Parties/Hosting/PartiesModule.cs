using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Parties.Hosting.DependencyInjection;

namespace Vitarish.Parties.Hosting;

/// <summary>
/// Module composition root for Parties registration.
/// </summary>
public sealed class PartiesModule : Module
{
    public override string Name => "Parties";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddParties(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => app.InitializeViews();
}
