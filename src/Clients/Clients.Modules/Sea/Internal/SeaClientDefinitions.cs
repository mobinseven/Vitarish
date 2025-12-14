using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

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

internal abstract class SeaHubClient<TDefinition>(IIdentityClientSignalRFactory connectionFactory) : IAsyncDisposable
    where TDefinition : ISignalRClientDefinition
{
    private HubConnection? connection;

    protected async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        connection ??= await connectionFactory.CreateConnection<TDefinition>(cancellationToken).ConfigureAwait(false);

        if (connection.State != HubConnectionState.Connected)
        {
            await connection.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }

    protected async Task InvokeAsync(string methodName, CancellationToken cancellationToken, params object?[] args)
    {
        HubConnection hubConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        await hubConnection.InvokeAsync(methodName, args, cancellationToken).ConfigureAwait(false);
    }

    protected async Task<TReturn> InvokeAsync<TReturn>(string methodName, CancellationToken cancellationToken, params object?[] args)
    {
        HubConnection hubConnection = await EnsureConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await hubConnection.InvokeAsync<TReturn>(methodName, args, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}
