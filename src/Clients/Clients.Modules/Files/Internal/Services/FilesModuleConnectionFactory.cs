using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Modules.Files.Models;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Files.Internal.Services;

internal class FilesModuleConnectionFactory(IIdentityClientSignalRFactory connectionFactory) : IEntityConnectionFactory<FileInformationDto>
{
    public async Task<HubConnection> CreateConnection()
    {
        HubConnection connection = await connectionFactory.CreateConnection<FilesModuleClientDefinition>();
        return connection;
    }
}
