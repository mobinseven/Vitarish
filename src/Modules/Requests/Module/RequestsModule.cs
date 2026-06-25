using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base;
using Vitarish.Requests.Api;
using Vitarish.Requests.Module.DependencyInjection;

namespace Vitarish.Requests.Module;

/// <summary>
/// Module composition root for Requests registration.
/// </summary>
public sealed class RequestsModule : Vitarish.Modules.Base.Module
{
    public override string Name => "Requests";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.AddRequests(configureDbContext ?? (_ => { }));

        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.MapRequestsApi();
        return Task.CompletedTask;
    }
}
