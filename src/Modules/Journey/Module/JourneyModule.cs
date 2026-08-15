using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Vitarish.Journey.Module.DependencyInjection;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Journey.Module;

public sealed class JourneyModule : Modules.Base.Core.Module
{
    public override string Name => "Journey";

    public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        builder.Services.AddJourneyModuleServices();
        return Task.CompletedTask;
    }

    public override Task Initialize(WebApplication app)
    {
        return Task.CompletedTask;
    }

    public override void MapEndpoints(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints) => endpoints.MapJourneyModuleEndpoints();
}
