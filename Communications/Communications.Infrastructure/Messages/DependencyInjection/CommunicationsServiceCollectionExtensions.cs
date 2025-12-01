namespace Vitarish.Communications.Infrastructure.Messages.DependencyInjection;

using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Base.Infrastructure.Pipeline;
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

        services.AddHttpClient<ISmsProvider, SmsDotIrProvider>();
        services.AddScoped<IMessagesService, MessagesService>();
        
        // Register FluentValidation validators from Core assembly
        services.AddValidatorsFromAssembly(typeof(Vitarish.Communications.Core.Messages.Application.Commands.SendSms).Assembly);
        
        // Register MediatR handlers with pipeline behaviors (validation and logging)
        services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        
        // Register health monitoring as a hosted service
        services.AddHostedService<IdentityHealthMonitorService>();

        return services;
    }
}
