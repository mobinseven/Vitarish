using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;

namespace Vorn.Ai.Server.Infrastructure.Services.Google;

public sealed class GoogleResponder(VornOptions vornOptions) : IResponder
{
    public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        throw new NotImplementedException("Google Gemini responder is not yet implemented. Please configure OpenAI or Ollama provider.");
    }

    public IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        throw new NotImplementedException("Google Gemini responder is not yet implemented. Please configure OpenAI or Ollama provider.");
    }
}
