using Vitarish.Intelligence.Contracts;
namespace Vitarish.Intelligence.Client;
public interface IIntelligenceClient
{
    Task<IntelligenceResult<ResponderResult>> RespondAsync(ResponderRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<IntelligenceStreamChunk<ResponderProgress>> StreamResponderAsync(ResponderRequest request, CancellationToken cancellationToken);
    Task<IntelligenceResult<TranscriptionResult>> TranscribeAsync(TranscriptionRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<IntelligenceStreamChunk<TranscriptionProgress>> StreamTranscriptionAsync(TranscriptionRequest request, CancellationToken cancellationToken);
    Task<IntelligenceResult<RewriteResult>> RewriteAsync(RewriteRequest request, CancellationToken cancellationToken);
    IAsyncEnumerable<IntelligenceStreamChunk<RewriteProgress>> StreamRewriteAsync(RewriteRequest request, CancellationToken cancellationToken);
}
