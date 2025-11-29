namespace Vorn.Ai.Server.Presentation.Hub;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Vorn.Ai.Server.Infrastructure.Services.OpenAi;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class SpeechHub(IAudioTranscriber audioTranscriber) : Hub
{
    public async IAsyncEnumerable<string> StreamTranscribeAsync(
        ChannelReader<byte[]> audio,
        string mimeType,
        string? language,
       [EnumeratorCancellation] CancellationToken cancellationToken, CancellationToken ct)
    {
        await foreach (string piece in audioTranscriber.StreamTranscribeAsync(audio, mimeType, language, cancellationToken))
        {
            yield return piece;
        }
    }
}
