
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Runtime.CompilerServices;
using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Interfaces;
using Vorn.Ai.Server.Infrastructure.Services.Dispatching;

namespace Vorn.Ai.Server.Presentation.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ResponseHub(IResponder responder, VornOptions vornOptions, ModelRegistry modelRegistry) : Hub
{
    public Task<AvailableModels> GetAvailableModelsAsync()
    {
        return Task.FromResult(modelRegistry.GetAvailableModels());
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach(ResponderOutput response in responder.StreamAsync(input, options, cancellationToken))
        {
            yield return response;
        }
    }

    public async IAsyncEnumerable<ResponderOutput> StreamRewriteAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyList<ResponderInput> input = [ResponderInput.TextInput(prompt)];
        await foreach(ResponderOutput response in responder.StreamAsync(input, new ResponderOptions { Instructions = vornOptions.Ai.QuickRewriteInstructions }, cancellationToken))
        {
            if(response.Type == ResponderOutputType.Text)
            {
                yield return response;
            }
        }
    }

}
