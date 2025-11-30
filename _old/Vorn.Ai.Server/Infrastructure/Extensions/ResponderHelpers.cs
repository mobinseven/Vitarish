using OpenAI.Responses;
using Vorn.Ai.Server.Domain.Models;

namespace Vorn.Ai.Server.Infrastructure.Extensions
{
    internal static class ResponderHelpers
    {
#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        internal static ResponseImageDetailLevel? MapImageDetail(ResponderInputImageDetail? detail)
        {
            return detail switch
            {
                ResponderInputImageDetail.Low => ResponseImageDetailLevel.Low,
                ResponderInputImageDetail.High => ResponseImageDetailLevel.High,
                ResponderInputImageDetail.Auto => ResponseImageDetailLevel.Auto,
                _ => null,
            };
        }

        internal static IEnumerable<ResponseItem> MapInput(IEnumerable<ResponderInput> inputs)
        {
            List<ResponseContentPart> parts = [];

            foreach(ResponderInput input in inputs)
            {
                if(input.Type == ResponderInputType.McpToolCallApproval)
                {
                    if(parts.Count > 0)
                    {
                        yield return ResponseItem.CreateUserMessageItem(parts);
                        parts = [];
                    }

                    yield return ResponseItem.CreateMcpApprovalResponseItem(input.ApprovalId!, true);
                    continue;
                }

                ResponseContentPart part = input.Type switch
                {
                    ResponderInputType.Text => ResponseContentPart.CreateInputTextPart(input.Text!),
                    ResponderInputType.Image => ResponseContentPart.CreateInputImagePart(new Uri(input.ImageUrl!), MapImageDetail(input.Detail)),
                    ResponderInputType.File when input.FileId is not null => ResponseContentPart.CreateInputFilePart(input.FileId),
                    ResponderInputType.File when input.FileData is not null && input.Filename is not null => ResponseContentPart.CreateInputFilePart(BinaryData.FromBytes(Convert.FromBase64String(input.FileData)), "application/octet-stream", input.Filename),
                    _ => throw new InvalidOperationException("Unsupported responder input type."),
                };

                parts.Add(part);
            }

            if(parts.Count > 0)
            {
                yield return ResponseItem.CreateUserMessageItem(parts);
            }
        }

        internal static IReadOnlyList<ResponderOutput> MapResponse(OpenAIResponse response)
        {
            List<ResponderOutput> outputs = [];
            List<ResponderOutput> aggregatedMessageParts = [];
            List<ResponderOutput> completionContent = [];

            foreach(ResponseItem item in response.OutputItems)
            {
                switch(item)
                {
                    case MessageResponseItem message:
                        foreach(ResponseContentPart? part in message.Content)
                        {
                            if(part.Kind == ResponseContentPartKind.OutputText && part.Text is not null)
                            {
                                ResponderOutput textOutput = ResponderOutput.TextOutput(part.Text);
                                outputs.Add(textOutput);
                                aggregatedMessageParts.Add(textOutput);
                            }
                        }
                        break;
                    case McpToolCallItem call:
                        ResponderOutput callOutput = ResponderOutput.McpToolCallOutput(
                            call.ToolName,
                            call.ToolArguments?.ToString(),
                            call.ServerLabel,
                            call.Id);
                        outputs.Add(callOutput);
                        completionContent.Add(callOutput);
                        break;
                    case McpToolDefinitionListItem list:
                        List<McpToolDescription> tools = list.ToolDefinitions
                            .Select(t => new McpToolDescription(t.Name, t.Description))
                            .ToList();
                        ResponderOutput listOutput = ResponderOutput.McpListToolsOutput(list.ServerLabel, tools);
                        outputs.Add(listOutput);
                        completionContent.Add(listOutput);
                        break;
                    case McpToolCallApprovalRequestItem approval:
                        ResponderOutput approvalOutput = ResponderOutput.McpApprovalRequestOutput(
                            approval.ToolName,
                            approval.ToolArguments?.ToString(),
                            approval.ServerLabel,
                            approval.Id);
                        outputs.Add(approvalOutput);
                        completionContent.Add(approvalOutput);
                        break;
                }
            }

            if(aggregatedMessageParts.Count > 0)
            {
                ResponderOutput messageOutput = ResponderOutput.MessageOutput([.. aggregatedMessageParts]);
                completionContent.Insert(0, messageOutput);
            }

            IReadOnlyDictionary<string, string>? metadata = null;

            object? metadataValue = response.GetType().GetProperty("Metadata")?.GetValue(response);

            if(metadataValue is IReadOnlyDictionary<string, string> stringMetadata)
            {
                metadata = stringMetadata;
            }
            else if(metadataValue is IReadOnlyDictionary<string, BinaryData> binaryMetadata)
            {
                metadata = binaryMetadata.ToDictionary(pair => pair.Key, pair => pair.Value.ToString());
            }
            else if(metadataValue is IEnumerable<KeyValuePair<string, object?>> objectMetadata)
            {
                Dictionary<string, string> snapshot = [];

                foreach(KeyValuePair<string, object?> entry in objectMetadata)
                {
                    snapshot[entry.Key] = entry.Value?.ToString() ?? string.Empty;
                }

                metadata = snapshot;
            }

            if(!string.IsNullOrEmpty(response.Id) || completionContent.Count > 0)
            {
                ResponderOutput completion = ResponderOutput.CompletionOutput(
                    response.Id,
                    [.. completionContent],
                    metadata);
                outputs.Add(completion);
            }

            return outputs;
        }
#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
    }
}
