using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace Vitarish.Main.Infrastructure.IdentityServer;

public class IdentityClientsStore : IClientStore
{
    public async Task<Client?> FindClientByIdAsync(string clientId) =>
        //AuthenticationClient? authenticationClient = await authClients.AuthenticationClients.SingleOrDefaultAsync(c => c.Id == clientId);
        //if (authenticationClient is not null)
        //{
        //    Client client = new()
        //    {
        //        EstateId = authenticationClient.Id,
        //        ClientName = authenticationClient.Title,
        //        ClientUri = authenticationClient.Url,
        //        AllowedGrantTypes = GrantTypes.Implicit,
        //        RedirectUris = { $"https://{authenticationClient.Url}/signin-oidc", $"https://www.{authenticationClient.Url}/signin-oidc" },
        //        PostLogoutRedirectUris = { $"https://{authenticationClient.Url}/signout-callback-oidc", $"https://www.{authenticationClient.Url}/signout-callback-oidc" },
        //        FrontChannelLogoutUri = $"{authenticationClient.Url}/signout-oidc",
        //        AccessTokenLifetime = (int)TimeSpan.FromDays(30).TotalSeconds,
        //        AllowOfflineAccess = true,
        //        AllowedScopes = { "openid", "profile", "email", "phone", IdentityDefinitions.PartyScope },
        //        Properties = { [nameof(authenticationClient.AutoAccess)] = authenticationClient.AutoAccess.ToString() }
        //    };
        //    return client;
        //}
        //ApiClient? apiClient = await apiClients.ApiClients.SingleOrDefaultAsync(c => c.Id == clientId);
        //if (apiClient is not null)
        //{
        //    // API clients don't have associated tenant/Owner claims at the entity level.
        //    // Tenant/Owner claims for API client tokens are provided at runtime by ApiClientTokenValidator
        //    // which resolves them from the HTTP request context (similar to PartyProfileService).
        //    Client client = new()
        //    {
        //        EstateId = apiClient.Id,
        //        ClientUri = apiClient.HostUri,
        //        ClientSecrets = [new Secret(apiClient.Secret.Sha256())],
        //        AllowedScopes = [apiClient.Scopes],
        //        AllowedGrantTypes = GrantTypes.ClientCredentials,
        //        ClientClaimsPrefix = string.Empty,
        //        Claims = []
        //    };
        //    return client;
        //}
        null;
}

