using Microsoft.AspNetCore.SignalR.Client;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Files.Client.Internal.Services;

public class FileTransferHub(ISignalRClientFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/files";
        registration.ResourceId = "/hubs/transfer";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        return connectionFactory.CreateConnection<FileTransferHub>(cancellationToken);
    }
}