using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Feed.Api;
using Vitarish.Feed.Infrastructure;
using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Feed.Module;

public sealed class FeedModule : Vitarish.Modules.Aggregator.Core.Module
{
    public override string Name => "Feed";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddFeedInfrastructure();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        _ = app.MapFeedApi();
        return Task.CompletedTask;
    }
}
