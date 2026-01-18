using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Hosting.Hubs;
using Vitarish.Communications.Hosting.Presentation.Api;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;
using Vitarish.Modules.Base;

namespace Vitarish.Communications.Hosting;

/// <summary>
/// Module wiring for the Communications stack using the shared module contracts.
/// </summary>
public class CommunicationModule : ModuleBase
{
    /// <inheritdoc />
    public override string Name => "Communications";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        _ = services.AddTransient<IMessagesService, MessagesService>();
        _ = services.AddTransient<ISmsProvider, SmsDotIrProvider>();
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMessagesEndpoints();
        endpoints.MapHub<MessagesHub>("/communications/hubs/messages");
    }
}
