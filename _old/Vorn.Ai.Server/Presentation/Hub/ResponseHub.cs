namespace Vorn.Ai.Server.Presentation.Hub;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vorn.Ai.Server.Domain.Models;
using Vorn.Ai.Server.Domain.Options;
using Vorn.Ai.Server.Infrastructure.Services.OpenAi;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public sealed class ResponseHub(IResponder responder, VornOptions vornOptions) : Hub
{
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
        await foreach (ResponderOutput response in responder.StreamAsync(input, new ResponderOptions { Instructions = vornOptions.Ai.QuickRewriteInstructions }, cancellationToken))
        {
            if (response.Type == ResponderOutputType.Text)
            {
                yield return response;
            }
        }
    }

}
