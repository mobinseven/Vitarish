using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Models;

namespace Vorn.Aaas.Client.Api.Internal;

internal interface IRequestIdentityForwarder
{
    ValueTask ForwardAsync(RequestIdentityDto envelope, IRequestIdentityContextWriter? writer = null, CancellationToken cancellationToken = default);
}

internal sealed class RequestIdentityForwarder(IServiceProvider services) : IRequestIdentityForwarder
{
    private IAaasClientFactory? factory;

    private IAaasClientFactory ClientFactory => factory ??= services.GetRequiredService<IAaasClientFactory>();

    public async ValueTask ForwardAsync(RequestIdentityDto envelope, IRequestIdentityContextWriter? writer = null, CancellationToken cancellationToken = default)
    {
        try
        {
            HttpClient client = await ClientFactory.CreateClient("/api/request-identities");
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                string.Empty,
                envelope,
                cancellationToken);

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Conflict)
            {
                var res = await response.Content.ReadFromJsonAsync<RequestIdentityDto>(cancellationToken);
                if (res is not null && writer is not null && response.IsSuccessStatusCode)
                {
                    RequestIdentityContextCommand command = new(
                        res.Authority,
                        res.ClientId,
                        res.OwnerId,
                        res.TenantId,
                        res.UserId,
                        res.ResourceId,
                        res.TraceId,
                        res.RequestId,
                        res.AdditionalInfo);
                    await writer.Write(command);
                }
                return;
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch
        {
            throw;
        }
    }
}
