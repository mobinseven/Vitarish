using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Modules.Intelligence.Internal.Services;

public class TranscriberClientDefinition(IIdentityClientSignalRFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/ai";
        registration.ResourceId = "/hubs/speech";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default) => connectionFactory.CreateConnection<TranscriberClientDefinition>(cancellationToken);
}

