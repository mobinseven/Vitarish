namespace Vorn.Ai.Client.Internal.Services;

using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Options;
using Vorn.Ai.Client.Abstractions;
public class AiSpeechHub(ISignalRClientFactory connectionFactory) : ISignalRClientDefinition
{
    public static void Configure(SignalRClientRegistration registration)
    {
        registration.ClientId = "/ai";
        registration.ResourceId = "/hubs/speech";
    }

    public Task<HubConnection> CreateAsync(CancellationToken cancellationToken = default)
    {
        return connectionFactory.CreateConnection<AiSpeechHub>(cancellationToken);
    }
}
internal sealed class SpeechHubClient(ISignalRClientFactory connectionFactory) : ISpeechHubClient, IAsyncDisposable
{
    private HubConnection? connection;

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        connection = await connectionFactory.CreateConnection<AiSpeechHub>();

        await connection.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio, string mimeType = "audio/webm", string? language = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await EnsureConnectionAsync(ct).ConfigureAwait(false);

        await foreach (string chunk in connection!.StreamAsync<string>(nameof(StreamTranscribeAsync), audio, mimeType, language, ct))
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

