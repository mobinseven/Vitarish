using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;

namespace Vitarish.Intelligence.Infrastructure.Services.Google;

public sealed class GoogleResponder(IOptionsMonitor<AiOptions> aiOptions) : IResponder
{
    public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => throw new NotImplementedException($"Google Gemini responder is not yet implemented (default model: {aiOptions.CurrentValue.DefaultResponseModel}).");

    public IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default) => throw new NotImplementedException($"Google Gemini responder is not yet implemented (default model: {aiOptions.CurrentValue.DefaultResponseModel}).");
}
