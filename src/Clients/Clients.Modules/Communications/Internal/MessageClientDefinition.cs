using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Modules.Communications.Internal;

public class MessageClientDefinition : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/communications";
        registration.ResourceId = "/hubs/messages";
    }
}
