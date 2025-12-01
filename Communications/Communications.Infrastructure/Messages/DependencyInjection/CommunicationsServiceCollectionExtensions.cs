namespace Vitarish.Communications.Infrastructure.Messages.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Core.Messages.Abstractions;
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

        services.AddSingleton<ISmsProvider, SmsDotIrProvider>();
        services.AddScoped<IMessagesService, MessagesService>();

        return services;
    }
}
