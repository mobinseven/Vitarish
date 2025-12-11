using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Microsoft.EntityFrameworkCore;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Domain.Entities;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Host.Infrastructure.IdentityServer;

namespace Vitarish.Host.Services;

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
                AllowedScopes = { "openid", "profile", "email", "phone", IdentityDefinitions.TenantOwnerScope },
                Properties = { [nameof(authenticationClient.AutoAccess)] = authenticationClient.AutoAccess.ToString() }
            };
            return client;
        }
        ApiClient? apiClient = await apiClients.ApiClients.SingleOrDefaultAsync(c => c.Id == clientId);
        if (apiClient is not null)
        {
            List<ClientClaim> claims = [];

            if (!string.IsNullOrEmpty(apiClient.TenantId))
            {
                claims.Add(new ClientClaim(IdentityDefinitions.TenantIdClaim, apiClient.TenantId));
            }

            if (!string.IsNullOrEmpty(apiClient.OwnerId))
            {
                claims.Add(new ClientClaim(IdentityDefinitions.OwnerIdClaim, apiClient.OwnerId));
            }

            Duende.IdentityServer.Models.Client client = new()
            {
                ClientId = apiClient.Id,
                ClientUri = apiClient.HostUri,
                ClientSecrets = [new Secret(apiClient.Secret.Sha256())],
                AllowedScopes = [apiClient.Scopes],
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientClaimsPrefix = string.Empty,
                Claims = claims
            };
            return client;
        }
        return null;
    }
}
