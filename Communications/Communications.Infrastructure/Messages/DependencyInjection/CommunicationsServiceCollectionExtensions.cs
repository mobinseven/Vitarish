namespace Vitarish.Communications.Infrastructure.Messages.DependencyInjection;

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Infrastructure.HealthMonitoring;
using Vitarish.Communications.Infrastructure.Messages.Options;
using Vitarish.Communications.Infrastructure.Messages.Providers.SmsDotIr;
using Vitarish.Communications.Infrastructure.Messages.Services;

public static class CommunicationsServiceCollectionExtensions
{
    public static IServiceCollection AddCommunicationsMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CommunicationsOptions>(
            configuration.GetSection(CommunicationsOptions.Section));
        
        services.AddSingleton(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CommunicationsOptions>>().Value);

        services.AddHttpClient<ISmsProvider, SmsDotIrProvider>();
        services.AddScoped<IMessagesService, MessagesService>();
        
        // Register MediatR handlers
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Register health monitoring as a hosted service
        services.AddHostedService<IdentityHealthMonitorService>();

        return services;
    }
}
