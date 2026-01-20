using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;
using Vitarish.Modules.Base;

namespace Vitarish.Communications.Hosting;

/// <summary>
/// Module wiring for the Communications stack using the shared module contracts.
/// </summary>
public class CommunicationModule : ModuleBase, ICommunicationsModule
{
    /// <inheritdoc />
    public override string Name => "Communications";

    /// <inheritdoc />
    protected override void ConfigureServices(
        IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDbContext)
    {
        _ = services.AddSingleton<ICommunicationsModule, CommunicationModule>();
        _ = services.AddTransient<IMessagesService, MessagesService>();
        _ = services.AddTransient<ISmsProvider, SmsDotIrProvider>();
    }
}
