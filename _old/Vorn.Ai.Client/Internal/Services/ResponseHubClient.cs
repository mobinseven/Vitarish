namespace Vorn.Ai.Client.Internal.Services;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;
using Vorn.Ai.Client.Abstractions;
using Vorn.Ai.Client.Models;
public class AiResponseHub(ISignalRClientFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/ai";
        registration.ResourceId = "/hubs/response";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        return connectionFactory.CreateConnection<AiResponseHub>(cancellationToken);
    }
}
internal sealed class ResponseHubClient(ISignalRClientFactory connectionFactory) : IResponseHubClient, IAsyncDisposable
{
    private HubConnection? connection;
    private async Task EnsureConnectionAsync(SignalRConnectionOptions? hubOptions = null, CancellationToken cancellationToken = default)
    {

        connection = await connectionFactory.CreateConnection<AiResponseHub>(hubOptions);
        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(
        IEnumerable<ResponderInput> input,
        ResponderOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(new SignalRConnectionOptions { UseMessagePack = false }, cancellationToken: cancellationToken).ConfigureAwait(false);
        await foreach (ResponderOutput chunk in connection!.StreamAsync<ResponderOutput>(nameof(StreamAsync), input, options, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async IAsyncEnumerable<ResponderOutput> StreamRewriteAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(new SignalRConnectionOptions { UseMessagePack = true }, cancellationToken).ConfigureAwait(false);
        await foreach (ResponderOutput chunk in connection!.StreamAsync<ResponderOutput>(nameof(StreamRewriteAsync), prompt, cancellationToken))
        {
            yield return chunk;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }
    }
}

