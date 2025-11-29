# Vorn.Ai.Client

Client library for the Vorn AI host. It provides services for interacting with AI endpoints.

## Getting started

1. Install the package:

```bash
dotnet add package Vorn.Ai.Client
```

2. Register dependencies and add the client:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IApiClientService, ApiClientService>(); // from Vorn.Aaas.Client.Api
builder.AddAiClient();
```

3. Consume the services:

```csharp
app.MapGet("/rewrite", (IAssistantService assistant) =>
    assistant.QuickRewriteAsync("Rewrite this text"));

var tool = new ResponderToolDefinition
{
    Type = "mcp",
    ServerLabel = "mcpqueries",
    ServerUrl = "https://your-host.example.com/mcp",
    AllowedTools = new[] { "querymcptool1", "querymcptool2" },
    RequireApproval = ResponderToolApprovalRequirement.Never
};

var options = new ResponderOptions { Tools = new[] { tool } };

app.MapPost("/response", (IResponseService responses, string prompt) 
    => responses.GenerateAsync([ResponderInput.TextInput(prompt)], options));

app.MapPost("/response/approve", (IResponseService responses, string id)
    => responses.GenerateAsync([ResponderInput.McpToolCallApprovalInput(id)], options));

app.MapPost("/response/stream", async (IResponseService responses, string prompt) =>
{
    await foreach (var item in responses.StreamAsync([ResponderInput.TextInput(prompt)], options))
    {
        Console.WriteLine($"{item.GetType().Name}: {System.Text.Json.JsonSerializer.Serialize(item)}");
    }
});

app.MapPost("/response/hub", async (IResponseHubClient hub, string prompt) =>
{
    await foreach (var item in hub.StreamAsync([ResponderInput.TextInput(prompt)], options))
    {
        Console.WriteLine($"{item.GetType().Name}: {System.Text.Json.JsonSerializer.Serialize(item)}");
    }
});

// Capture the completion output to continue the conversation.
app.MapPost("/response/follow-up", async (IResponseService responses, string prompt, string? previousId) =>
{
    var followUpOptions = options with { PreviousResponseId = previousId };
    var outputs = await responses.GenerateAsync([ResponderInput.TextInput(prompt)], followUpOptions);
    var completion = outputs.FirstOrDefault(o => o.Type == ResponderOutputType.Completion);
    return new
    {
        Outputs = outputs,
        NextPreviousResponseId = completion?.Id
    };
});

app.MapPost("/response/hub/rewrite", async (IResponseHubClient hub, string prompt) =>
{
    await foreach (var item in hub.StreamRewriteAsync(prompt))
    {
        Console.WriteLine($"{item.GetType().Name}: {System.Text.Json.JsonSerializer.Serialize(item)}");
    }
});

app.MapPost("/speech", async (ISpeechToTextService speech, IFormFile file) =>
{
    using var stream = file.OpenReadStream();
    return await speech.TranscribeAsync(stream, mimeType: file.ContentType);
});

app.MapPost("/speech/hub", async (ISpeechHubClient hub, IFormFile file) =>
{
    using var stream = file.OpenReadStream();
    return await hub.TranscribeAsync(stream, mimeType: file.ContentType);
});
```

`IResponseService` generates model responses, `IResponseHubClient` streams them via SignalR, `IAssistantService` handles structured prompts and quick rewrites, `ISpeechToTextService` transcribes audio streams, and `ISpeechHubClient` transcribes audio via SignalR.

`ResponderOptions.PreviousResponseId` can be supplied with the identifier returned by a `ResponderOutputType.Completion` payload to start a follow-up turn. Reset it to `null` to begin a new conversation thread.

`ResponderOptions.Model` overrides the configured default model on a per-call basis. Leave it `null` to continue using the server's configured default.

