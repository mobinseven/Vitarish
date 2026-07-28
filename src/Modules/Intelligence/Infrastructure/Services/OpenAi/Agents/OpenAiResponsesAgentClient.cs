using OpenAI.Responses;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

internal sealed class OpenAiResponsesAgentClient(
    string apiKey,
    OpenAiAgentRequestMapper requestMapper,
    OpenAiAgentResponseMapper responseMapper) : IOpenAiAgentClient
{
#pragma warning disable OPENAI001
    public async Task<OpenAiAgentResponse> CreateResponseAsync(
        OpenAiAgentRequest request,
        CancellationToken cancellationToken)
    {
        ResponsesClient client = new(apiKey);
        ResponseResult response = await client.CreateResponseAsync(
            requestMapper.Map(request),
            cancellationToken);
        return responseMapper.Map(response);
    }
#pragma warning restore OPENAI001
}
