using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Links.Application.Abstractions;
using Vitarish.Links.Infrastructure.Data;
using Vitarish.Links.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;

namespace Vitarish.Links.Interface;

public sealed class LinksModule : Module, ILinksModule
{
    public override string Name => "Links";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishLinksDomainEntityHandlers();

        _ = builder.Services.AddSingleton<ILinksModule, LinksModule>();

        _ = builder.Services.AddDbContext<LinksDbContext>((_, options) => configureDbContext?.Invoke(options), ServiceLifetime.Transient);

        _ = builder.Services.AddLinksInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
