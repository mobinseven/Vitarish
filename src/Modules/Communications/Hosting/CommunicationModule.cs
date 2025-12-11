using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Hosting.Presentation.Api;
using Vitarish.Communications.Hosting.Hubs;
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
        Action<DbContextOptionsBuilder>? configureDbContext) =>
        services.AddCommunications(configuration); // Placeholder for future options binding customization specific to the module lifecycle.

    /// <inheritdoc />
    protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMessagesEndpoints();
        endpoints.MapHub<MessagesHub>("/hubs/messages");
    }

    /// <inheritdoc />
    protected override void ConfigureHealthChecks(WebApplicationBuilder builder) =>
        // Reserved for Communications-specific health checks (e.g., SMS gateway reachability).
        builder.Services.AddHealthChecks();
}
