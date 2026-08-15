using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Feed.Api;
using Vitarish.Feed.Infrastructure;

namespace Vitarish.Feed.Module;

public sealed class FeedModule : Modules.Base.Core.Module
{
    public override string Name => "Feed";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = builder.Services.AddFeedInfrastructure();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        return Task.CompletedTask;
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapFeedApi();
}
