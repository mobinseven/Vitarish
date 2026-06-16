using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Links.Application.Abstractions;
using Vitarish.Links.Infrastructure.Data;
using Vitarish.Links.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Links.Module;

public sealed class LinksModule : Vitarish.Modules.Base.Module, ILinksModule
{
    public override string Name => "Links";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishLinksDomainEntityHandlers();

        _ = builder.Services.AddSingleton<ILinksModule, LinksModule>();

        _ = builder.Services.AddTransientDbContext<LinksDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddLinksInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => app.MigrateDatabaseAsync<LinksDbContext>();
}
