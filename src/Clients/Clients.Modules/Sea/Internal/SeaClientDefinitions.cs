using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;
using Vorn.Entities.Client.SignalR;
using Vorn.Entities.Client.Models;

namespace Vitarish.Clients.Modules.Sea.Internal;

internal static class SeaClientDefinition
{
    internal const string ClientId = "/sea";

    internal static void Configure(SignalRClientRegistration registration, string resourceId)
    {
        registration.ClientId = ClientId;
        registration.ResourceId = resourceId;
    }
}

public class ProjectsClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/projects");
}

public class ProceduresClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/procedures");
}

public class EventsClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/events");
}

public class AttachmentsClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/attachments");
}

public class EventRolesClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/event-roles");
}

public class RolesClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/roles");
}

public class UserRolesClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/user-roles");
}

public class ReceiptsClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/receipts");
}

public class RemindersClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/reminders");
}

public class UsageConfigurationClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration) =>
        SeaClientDefinition.Configure(registration, "/hubs/usage-configuration");
}

internal sealed class SeaEntityConnectionFactory<TEntityDto, TDefinition>(IIdentityClientSignalRFactory connectionFactory)
    : IEntityConnectionFactory<TEntityDto>
    where TEntityDto : EntityDto
    where TDefinition : ISignalRClientDefinition
{
    public async Task<HubConnection> CreateConnection()
    {
        HubConnection connection = await connectionFactory.CreateConnection<TDefinition>().ConfigureAwait(false);
        return connection;
    }
}

internal abstract class SeaEntityClient<TEntityDto, TEntityDescriptorDto, TDefinition>(
    IIdentityClientSignalRFactory connectionFactory,
    IEntityInterceptionProvider entityInterceptionProvider)
    : EntityClient<TEntityDto, TEntityDescriptorDto>(
        new SeaEntityConnectionFactory<TEntityDto, TDefinition>(connectionFactory),
        entityInterceptionProvider),
        IAsyncDisposable
    where TEntityDto : EntityDto
    where TEntityDescriptorDto : EntityDescriptorDto
    where TDefinition : ISignalRClientDefinition
{
}
