using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Infrastructure.Data;
using Vitarish.Contents.Infrastructure.DependencyInjection;
using Vitarish.Modules.Base;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.Contents.Module;

public sealed class ContentsModule : Vitarish.Modules.Base.Module, IContentsModule
{
    public override string Name => "Contents";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddVitarishContentsDomainEntityHandlers();

        _ = builder.Services.AddSingleton<IContentsModule, ContentsModule>();
        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddTransientMultiTenantDbContext<ContentsDbContext>((_, options) => configureDbContext?.Invoke(options));

        _ = builder.Services.AddContentsInfrastructure();

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => app.MigrateDatabaseAsync<ContentsDbContext>();
}
