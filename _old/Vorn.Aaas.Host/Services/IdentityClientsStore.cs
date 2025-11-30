using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
namespace Vorn.Aaas.Host.Services;

public class IdentityClientsStore(AuthenticationClientDbContext authClients, ApiClientDbContext apiClients) : IClientStore
{
    public async Task<Duende.IdentityServer.Models.Client?> FindClientByIdAsync(string clientId)
    {
        AuthenticationClient? authenticationClient = await authClients.AuthenticationClients.SingleOrDefaultAsync(c => c.Id == clientId);
        if (authenticationClient is not null)
        {
            Duende.IdentityServer.Models.Client client = new()
            {
                ClientId = authenticationClient.Id,
                ClientName = authenticationClient.Title,
                ClientUri = authenticationClient.Url,
                AllowedGrantTypes = GrantTypes.Implicit,
                RedirectUris = { $"https://{authenticationClient.Url}/signin-oidc", $"https://www.{authenticationClient.Url}/signin-oidc" },
                PostLogoutRedirectUris = { $"https://{authenticationClient.Url}/signout-callback-oidc", $"https://www.{authenticationClient.Url}/signout-callback-oidc" },
                FrontChannelLogoutUri = $"{authenticationClient.Url}/signout-oidc",
                AccessTokenLifetime = (int)TimeSpan.FromDays(30).TotalSeconds,
                AllowOfflineAccess = true,
                AllowedScopes = { "openid", "profile", "email", "phone" },
                Properties = { [nameof(authenticationClient.AutoAccess)] = authenticationClient.AutoAccess.ToString() }
            };
            return client;
        }
        ApiClient? apiClient = await apiClients.ApiClients.SingleOrDefaultAsync(c => c.Id == clientId);
        if (apiClient is not null)
        {
            Duende.IdentityServer.Models.Client client = new()
            {
                ClientId = apiClient.Id,
                ClientUri = apiClient.HostUri,
                ClientSecrets = [new Secret(apiClient.Secret.Sha256())],
                AllowedScopes = [apiClient.Scopes],
                AllowedGrantTypes = GrantTypes.ClientCredentials
            };
            return client;
        }
        return null;
    }
}
