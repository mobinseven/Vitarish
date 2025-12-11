using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Modules.Files.Internal.Services;

public class FilesModuleClientDefinition(IIdentityClientSignalRFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/files";
        registration.ResourceId = "/hubs/transfer";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default) => connectionFactory.CreateConnection<FilesModuleClientDefinition>(cancellationToken);
}
