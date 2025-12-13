using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.CompilerServices;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Modules.Intelligence.Abstractions;
using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Internal.Services;
internal sealed class ResponderClient(IIdentityClientSignalRFactory connectionFactory) : IResponderClient, IAsyncDisposable
{
    private HubConnection? connection;
    private async Task EnsureConnectionAsync(SignalRConnectionOptions? hubOptions = null, CancellationToken cancellationToken = default)
    {

        connection = await connectionFactory.CreateConnection<ResponderClientDefinition>(hubOptions);
        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AvailableModels> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(new SignalRConnectionOptions { UseMessagePack = false }, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await connection!.InvokeAsync<AvailableModels>(nameof(GetAvailableModelsAsync), cancellationToken).ConfigureAwait(false);
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

