using Microsoft.AspNetCore.SignalR.Client;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Modules.Intelligence.Internal.Services;

public class ResponderClientDefinition(IIdentityClientSignalRFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/ai";
        registration.ResourceId = "/hubs/response";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default) => connectionFactory.CreateConnection<ResponderClientDefinition>(cancellationToken);
}

