using Microsoft.AspNetCore.SignalR.Client;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Options;
using Vorn.Ai.Client.Abstractions;
using Vorn.Ai.Client.Contracts.Transcription;
using Vorn.Ai.Client.Models;

namespace Vorn.Ai.Client.Internal.Services;

public class TranscriberClientDefinition(ISignalRClientFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/ai";
        registration.ResourceId = "/hubs/speech";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        return connectionFactory.CreateConnection<TranscriberClientDefinition>(cancellationToken);
    }
}
internal sealed class TranscriberClient(ISignalRClientFactory connectionFactory) : ITranscriberClient, IAsyncDisposable
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

