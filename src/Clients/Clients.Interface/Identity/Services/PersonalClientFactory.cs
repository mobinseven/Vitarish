using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Vitarish.Clients.Identity.Extensions;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Interface.Identity.Extensions;

namespace Vitarish.Clients.Interface.Identity.Services;

internal class PersonalClientFactory(IHttpClientFactory httpClientFactory,
    IPersonalAccessTokenClient personalAccessTokenClient,
    AuthenticationStateProvider authenticationState,
    IOptions<IdentityClientOptions> options) : IPersonalClientFactory
{
    public async Task<HttpClient> CreateAuthenticatedClient(string? endpoint = null)
    {
        ClaimsPrincipal user = (await authenticationState.GetAuthenticationStateAsync()).User;
        string userId = user.GetId() ?? throw new InvalidOperationException("Can not create personal access token without a valid user ID.");
        string pat = await personalAccessTokenClient.CreateTokenAsync(userId) ?? throw new InvalidOperationException("Can not create an authenticated client without a valid token.");
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(endpoint is null ? options.Value.Authority : UrlHelper.UrlCombine(options.Value.Authority, endpoint));
        httpClient.SetBearerToken(pat);
        return httpClient;
    }
}
