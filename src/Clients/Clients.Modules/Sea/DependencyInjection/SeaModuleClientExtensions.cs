using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.DependencyInjection;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;
using Vitarish.Clients.Modules.Sea.Internal.Services;

namespace Vitarish.Clients.Modules.Sea.DependencyInjection;

public static class SeaModuleClientExtensions
{
    public static IServiceCollection AddSeaModuleClient(this IServiceCollection services)
    {
        AddSeaClient<IProjectsClient, ProjectsClient, ProjectsClientDefinition>(services);
        AddSeaClient<IProceduresClient, ProceduresClient, ProceduresClientDefinition>(services);
        AddSeaClient<IEventsClient, EventsClient, EventsClientDefinition>(services);
        AddSeaClient<IAttachmentsClient, AttachmentsClient, AttachmentsClientDefinition>(services);
        AddSeaClient<IEventRolesClient, EventRolesClient, EventRolesClientDefinition>(services);
        AddSeaClient<IRolesClient, RolesClient, RolesClientDefinition>(services);
        AddSeaClient<IUserRolesClient, UserRolesClient, UserRolesClientDefinition>(services);
        AddSeaClient<IReceiptsClient, ReceiptsClient, ReceiptsClientDefinition>(services);
        AddSeaClient<IRemindersClient, RemindersClient, RemindersClientDefinition>(services);
        AddSeaClient<IUsageConfigurationClient, UsageConfigurationClient, UsageConfigurationClientDefinition>(services);

        return services;
    }

    public static WebApplicationBuilder AddSeaModuleClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddSeaModuleClient();
        return builder;
    }

    private static void AddSeaClient<TAbstraction, TImplementation, TDefinition>(IServiceCollection services)
        where TAbstraction : class
        where TImplementation : class, TAbstraction
        where TDefinition : ISignalRClientDefinition
    {
        _ = services.AddScoped<TAbstraction, TImplementation>();
        _ = services.AddIdentitySignalRClient<TDefinition>();
    }
}
