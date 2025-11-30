using Duende.IdentityServer.Models;

namespace Vorn.Aaas.Host.IdentityServer;

internal static partial class IdentityDefinitions
{
    public static IdentityResource[] IdentityResources = [
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone()
            ];
}
