namespace Vorn.Interface.Components;

using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vorn.Ai.Client.Models;

public partial class AiJsonGeneratorChatInput : AiResponderChatInput
{
    private readonly List<ResponderOutput> _bufferedOutputs = [];
    private EventCallback<string?> _onError = EventCallback<string?>.Empty;
    private EventCallback<bool> _onBusyChanged = EventCallback<bool>.Empty;

    [Parameter]
    public string Instructions { get; set; } = """
        Always reply with a JSON object that matches the provided schema. Populate every required field using the supplied description and conversation. Use ISO-8601 format (yyyy-MM-dd) for dates. When information is not available, return null for optional fields. When revisions are requested, update the JSON to reflect the latest information. Do not include Markdown or commentary.
        """;

    [Parameter, EditorRequired]
    public string Schema { get; set; } = default!;

    [Parameter]
    public new string? InitializationPrompt { get; set; }
        = "We'll collaborate through a conversation. Ask for any missing details you need.";

    [Parameter]
    public EventCallback<AiJsonGeneratedEventArgs> OnJsonReceived { get; set; }
        = EventCallback<AiJsonGeneratedEventArgs>.Empty;

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        if(parameters.TryGetValue(nameof(OnError), out EventCallback<string?> onError))
        {
            _onError = onError;
        }

        if(parameters.TryGetValue(nameof(OnBusyChanged), out EventCallback<bool> onBusyChanged))
        {
            _onBusyChanged = onBusyChanged;
        }

        await base.SetParametersAsync(parameters);

        OnError = EventCallback.Factory.Create<string?>(this, HandleErrorAsync);
        OnBusyChanged = EventCallback.Factory.Create<bool>(this, HandleBusyChangedAsync);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        SetInitializationPrompt(BuildInitializationPrompt());
    }

    protected override ResponderOptions BuildEffectiveOptions(ResponderOptions options)
    {
        ResponderOptions effective = base.BuildEffectiveOptions(options);

        return effective with
        {
            Instructions = BuildInstructions(options),
            TextFormat = ResponseTextFormat.JsonObject
        };
    }

    protected override async Task HandleOutputAsync(ResponderOutput output)
    {
        _bufferedOutputs.Add(output);

        if(output.Type == ResponderOutputType.Text && !string.IsNullOrWhiteSpace(output.Text))
        {
            await OnJsonReceived.InvokeAsync(new AiJsonGeneratedEventArgs(output.Text!, false));
        }

        await base.HandleOutputAsync(output);
    }

    protected override async Task HandleOutputEndAsync(IEnumerable<ResponderOutput> outputs)
    {
        List<ResponderOutput> materialized = outputs as List<ResponderOutput>
            ?? outputs.ToList();

        string text = materialized.LastOrDefault(o => o.Type == ResponderOutputType.Text)?.Text
            ?? _bufferedOutputs.LastOrDefault(o => o.Type == ResponderOutputType.Text)?.Text
            ?? string.Empty;

        _bufferedOutputs.Clear();

        await OnJsonReceived.InvokeAsync(new AiJsonGeneratedEventArgs(text, true));

        await base.HandleOutputEndAsync(materialized);
    }

    private async Task HandleErrorAsync(string? message)
    {
        _bufferedOutputs.Clear();

        if(_onError.HasDelegate)
        {
            await _onError.InvokeAsync(message);
        }
    }

    private async Task HandleBusyChangedAsync(bool busy)
    {
        if(busy)
        {
            _bufferedOutputs.Clear();
        }

        if(_onBusyChanged.HasDelegate)
        {
            await _onBusyChanged.InvokeAsync(busy);
        }
    }

    private string BuildInstructions(ResponderOptions options)
    {
        List<string> instructions = [];

        if(!string.IsNullOrWhiteSpace(Instructions))
        {
            instructions.Add(Instructions.Trim());
        }

        if(!string.IsNullOrWhiteSpace(options.Instructions))
        {
            instructions.Add(options.Instructions.Trim());
        }

        return string.Join($"{Environment.NewLine}{Environment.NewLine}", instructions);
    }

    private string BuildInitializationPrompt()
    {
        string schemaText = string.IsNullOrWhiteSpace(Schema)
            ? "No schema was provided."
            : Schema;

        string schemaPrompt = $"Generate a JSON object that conforms to this schema:\n{schemaText}\nUse the conversation history to gather missing information and revise the JSON when needed.";

        if(string.IsNullOrWhiteSpace(InitializationPrompt))
        {
            return schemaPrompt;
        }

        return $"{InitializationPrompt!.Trim()}\n\n{schemaPrompt}";
    }

    public readonly record struct AiJsonGeneratedEventArgs(string Json, bool IsFinal);
}
