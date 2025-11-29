using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Entities.Client.SignalR;
using Vorn.Files.Client.Models;

namespace Vorn.Files.Client.Internal.Services;

internal class ConnectionFactory(ISignalRClientFactory connectionFactory) : IEntityConnectionFactory<FileInformationDto>
{
    public async Task<HubConnection> CreateConnection()
    {
        HubConnection connection = await connectionFactory.CreateConnection<FileTransferHub>();
        return connection;
    }
}
