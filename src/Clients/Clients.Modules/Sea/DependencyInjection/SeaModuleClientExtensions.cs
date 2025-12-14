using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.DependencyInjection;
using Vitarish.Clients.Modules.Sea.Abstractions;
using Vitarish.Clients.Modules.Sea.Internal;

namespace Vitarish.Clients.Modules.Sea.DependencyInjection;

public static class SeaModuleClientExtensions
{
    public static IServiceCollection AddSeaModuleClient(this IServiceCollection services)
    {
        _ = services.AddScoped<IProjectsClient, ProjectsClient>();
        _ = services.AddScoped<IProceduresClient, ProceduresClient>();
        _ = services.AddScoped<IEventsClient, EventsClient>();
        _ = services.AddScoped<IAttachmentsClient, AttachmentsClient>();
        _ = services.AddScoped<IEventRolesClient, EventRolesClient>();
        _ = services.AddScoped<IRolesClient, RolesClient>();
        _ = services.AddScoped<IUserRolesClient, UserRolesClient>();
        _ = services.AddScoped<IReceiptsClient, ReceiptsClient>();
        _ = services.AddScoped<IRemindersClient, RemindersClient>();
        _ = services.AddScoped<IUsageConfigurationClient, UsageConfigurationClient>();

        _ = services.AddIdentitySignalRClient<ProjectsClientDefinition>();
        _ = services.AddIdentitySignalRClient<ProceduresClientDefinition>();
        _ = services.AddIdentitySignalRClient<EventsClientDefinition>();
        _ = services.AddIdentitySignalRClient<AttachmentsClientDefinition>();
        _ = services.AddIdentitySignalRClient<EventRolesClientDefinition>();
        _ = services.AddIdentitySignalRClient<RolesClientDefinition>();
        _ = services.AddIdentitySignalRClient<UserRolesClientDefinition>();
        _ = services.AddIdentitySignalRClient<ReceiptsClientDefinition>();
        _ = services.AddIdentitySignalRClient<RemindersClientDefinition>();
        _ = services.AddIdentitySignalRClient<UsageConfigurationClientDefinition>();

        return services;
    }

    public static WebApplicationBuilder AddSeaModuleClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddSeaModuleClient();
        return builder;
    }
}
