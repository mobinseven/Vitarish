using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Communications.Internal;
using Vitarish.Clients.Modules.Files.Internal.Services;
using Vitarish.Clients.Modules.Intelligence.Internal.Services;
using Vitarish.Clients.Modules.Sea.Internal;

namespace Vitarish.Clients.Modules;

public interface IHubConnectionStarter
{
    Task StartAsync(HubConnection connection, CancellationToken cancellationToken = default);
    Task DisposeAsync(HubConnection connection);
}

internal sealed class HubConnectionStarter : IHubConnectionStarter
{
    public Task StartAsync(HubConnection connection, CancellationToken cancellationToken = default) =>
        connection.StartAsync(cancellationToken);

    public Task DisposeAsync(HubConnection connection) => connection.DisposeAsync().AsTask();
}

public interface IModulesClientsSelfTest
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed class ModulesClientsSelfTest(
    IIdentityClientSignalRFactory connectionFactory,
    IHubConnectionStarter connectionStarter,
    ILogger<ModulesClientsSelfTest> logger) : IHostedService, IModulesClientsSelfTest
{
    private readonly IReadOnlyList<(string Name, Func<CancellationToken, Task<HubConnection>> ConnectionFactory)> probes =
    [
        ("Communications", connectionFactory.CreateConnection<MessageClientDefinition>),
        ("Files", connectionFactory.CreateConnection<FilesModuleClientDefinition>),
        ("Intelligence.Responder", connectionFactory.CreateConnection<ResponderClientDefinition>),
        ("Intelligence.Transcriber", connectionFactory.CreateConnection<TranscriberClientDefinition>),
        ("Sea.Projects", connectionFactory.CreateConnection<ProjectsClientDefinition>),
        ("Sea.Procedures", connectionFactory.CreateConnection<ProceduresClientDefinition>),
        ("Sea.Events", connectionFactory.CreateConnection<EventsClientDefinition>),
        ("Sea.Attachments", connectionFactory.CreateConnection<AttachmentsClientDefinition>),
        ("Sea.EventRoles", connectionFactory.CreateConnection<EventRolesClientDefinition>),
        ("Sea.Roles", connectionFactory.CreateConnection<RolesClientDefinition>),
        ("Sea.UserRoles", connectionFactory.CreateConnection<UserRolesClientDefinition>),
        ("Sea.Receipts", connectionFactory.CreateConnection<ReceiptsClientDefinition>),
        ("Sea.Reminders", connectionFactory.CreateConnection<RemindersClientDefinition>),
        ("Sea.UsageConfiguration", connectionFactory.CreateConnection<UsageConfigurationClientDefinition>)
    ];

    public Task StartAsync(CancellationToken cancellationToken) => RunAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        foreach ((string name, Func<CancellationToken, Task<HubConnection>> probe) in probes)
        {
            await ProbeAsync(name, probe, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ProbeAsync(
        string moduleName,
        Func<CancellationToken, Task<HubConnection>> connectionFactory,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting module client probe for {ModuleClient}", moduleName);

        try
        {
            HubConnection connection = await connectionFactory(cancellationToken).ConfigureAwait(false);

            try
            {
                await connectionStarter.StartAsync(connection, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Module client probe for {ModuleClient} succeeded", moduleName);
            }
            finally
            {
                await connectionStarter.DisposeAsync(connection).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Module client probe for {ModuleClient} failed", moduleName);
            throw new InvalidOperationException($"Module client probe for {moduleName} failed: {ex.Message}", ex);
        }
    }
}
