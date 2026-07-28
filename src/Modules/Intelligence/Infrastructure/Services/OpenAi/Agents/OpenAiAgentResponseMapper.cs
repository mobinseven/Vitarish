using System.Text;
using OpenAI.Responses;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

public sealed class OpenAiAgentResponseMapper
{
#pragma warning disable OPENAI001
    internal OpenAiAgentResponse Map(ResponseResult response)
    {
        StringBuilder output = new();
        List<AgentToolCall> calls = [];

        foreach (ResponseItem item in response.OutputItems)
        {
            switch (item)
            {
                case MessageResponseItem message:
                    foreach (ResponseContentPart part in message.Content.Where(
                        static part => part.Kind == ResponseContentPartKind.OutputText))
                    {
                        _ = output.Append(part.Text);
                    }
                    break;
                case FunctionCallResponseItem call:
                    calls.Add(new AgentToolCall(
                        call.CallId,
                        call.FunctionName,
                        call.FunctionArguments.ToString()));
                    break;
            }
        }

        return new OpenAiAgentResponse(
            response.Id,
            output.Length == 0 ? null : output.ToString(),
            calls,
            response.Usage?.InputTokenCount ?? 0,
            response.Usage?.OutputTokenCount ?? 0);
    }
#pragma warning restore OPENAI001
}
