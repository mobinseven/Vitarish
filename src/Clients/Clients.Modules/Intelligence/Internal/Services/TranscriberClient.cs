using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Modules.Intelligence.Abstractions;
using Vitarish.Clients.Modules.Intelligence.Contracts.Transcription;
using Vitarish.Clients.Modules.Intelligence.Models;

namespace Vitarish.Clients.Modules.Intelligence.Internal.Services;
internal sealed class TranscriberClient(IIdentityClientSignalRFactory connectionFactory) : ITranscriberClient, IAsyncDisposable
{
    private HubConnection? connection;

    private async Task EnsureConnectionAsync(SignalRConnectionOptions? hubOptions = null, CancellationToken cancellationToken = default)
    {
        connection = await connectionFactory.CreateConnection<TranscriberClientDefinition>(hubOptions);

        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AvailableModels> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureConnectionAsync(new SignalRConnectionOptions { UseMessagePack = false }, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await connection!.InvokeAsync<AvailableModels>(nameof(GetAvailableModelsAsync), cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType = "audio/webm",
        string? language = null,
        TranscriberOptions? options = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await EnsureConnectionAsync(cancellationToken: ct).ConfigureAwait(false);

        await foreach (string chunk in connection!.StreamAsync<string>(nameof(StreamTranscribeAsync), audio, mimeType, language, options, ct))
        {
            yield return chunk;
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
        }
    }
}

