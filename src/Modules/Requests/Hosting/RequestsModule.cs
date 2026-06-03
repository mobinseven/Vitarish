using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Requests.Hosting.DependencyInjection;

namespace Vitarish.Requests.Hosting;

/// <summary>
/// Module composition root for Requests registration.
/// </summary>
public sealed class RequestsModule : Module
{
    public override string Name => "Requests";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddRequests(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app) => Task.CompletedTask;
}
