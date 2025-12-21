using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Contracts;

namespace Vitarish.Clients.Identity.Internal;

internal sealed class RequestContextClient(IIdentityClientService identityClientService) : IRequestContextClient
{

    public RequestDto? Request { get; private set; }
    public async Task Initialize()
    {
        HttpClient client = await identityClientService.CreateClient().ConfigureAwait(false);
        using HttpResponseMessage response = await client.GetAsync($"/request").ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        Request = await response.Content.ReadFromJsonAsync<RequestDto>().ConfigureAwait(false);

    }
}
