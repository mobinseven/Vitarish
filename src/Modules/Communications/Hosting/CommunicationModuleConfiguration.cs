
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Communications.Domain.Options;
using Vitarish.Communications.Infrastructure.Services;
using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

namespace Vitarish.Communications.Hosting;

public static class CommunicationModuleConfiguration
{
    public static IServiceCollection AddCommunications(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddTransient<IMessagesService, MessagesService>();
        _ = services.AddTransient<ISmsProvider, SmsDotIrProvider>();
        _ = services.AddSignalR().AddMessagePackProtocol();
        return services;
    }
}
