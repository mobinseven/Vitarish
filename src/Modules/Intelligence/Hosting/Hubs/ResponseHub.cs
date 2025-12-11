using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Hosting.Hubs;

public sealed class ResponseHub(IResponder responder, IOptionsMonitor<AiOptions> options, ModelRegistry modelRegistry) : Hub
{
    public Task<AvailableModels> GetAvailableModelsAsync() => Task.FromResult(modelRegistry.GetAvailableModels());

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (ResponderOutput response in responder.StreamAsync(input, options, cancellationToken))
        {
            yield return response;
        }
    }

    public async IAsyncEnumerable<ResponderOutput> StreamRewriteAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyList<ResponderInput> input = [ResponderInput.TextInput(prompt)];
        await foreach (ResponderOutput response in responder.StreamAsync(input, new ResponderOptions { Instructions = options.CurrentValue.QuickRewriteInstructions }, cancellationToken))
        {
            if (response.Type == ResponderOutputType.Text)
            {
                yield return response;
            }
        }
    }

}
