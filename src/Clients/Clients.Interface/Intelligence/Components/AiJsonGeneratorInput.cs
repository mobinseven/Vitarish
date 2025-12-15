namespace Vitarish.Clients.Interface.Intelligence.Components;

public class AiJsonGeneratorInput : AiResponderInput
{
    private readonly List<ResponderOutput> bufferedOutputs = [];
    private EventCallback<ResponderOutput> externalOnOutput;
    private EventCallback<IEnumerable<ResponderOutput>> externalOnOutputEnd;
    private EventCallback<string?> externalOnError;
    private EventCallback<bool> externalOnBusyChanged;
    private string Instructions { get; set; } = @"Always reply with a JSON object that matches the provided schema. Populate every required field using the supplied patient description. Use ISO-8601 format (yyyy-MM-dd) for dates. When information is not available, return null for optional fields. Do not include Markdown or commentary.";

    [Parameter, EditorRequired]
    public string Schema { get; set; } = default!;

    [Parameter]
    public EventCallback<AiJsonGeneratedEventArgs> OnJsonReceived { get; set; }
        = EventCallback<AiJsonGeneratedEventArgs>.Empty;

    protected override void OnParametersSet()
    {
        externalOnOutput = OnOutput;
        externalOnOutputEnd = OnOutputEnd;
        externalOnError = OnError;
        externalOnBusyChanged = OnBusyChanged;

        ResponderOptions baseOptions = Options ?? new ResponderOptions();
        Options = baseOptions with
        {
            Instructions = Instructions,
            TextFormat = ResponseTextFormat.JsonObject
        };

        OnOutput = EventCallback.Factory.Create<ResponderOutput>(this, HandleOutputAsync);
        OnOutputEnd = EventCallback.Factory.Create<IEnumerable<ResponderOutput>>(this, HandleCompletedAsync);
        OnError = EventCallback.Factory.Create<string?>(this, ForwardErrorAsync);
        OnBusyChanged = EventCallback.Factory.Create<bool>(this, HandleBusyChangedAsync);

        base.OnParametersSet();

        SetInitializationPrompt($"Generate a JSON object that conforms to this schema: \n {Schema} \n Based on the following prompt/description: \n ");
    }

    private async Task HandleOutputAsync(ResponderOutput output)
    {
        bufferedOutputs.Add(output);

        if (output.Type == ResponderOutputType.Text && !string.IsNullOrWhiteSpace(output.Text))
        {
            await OnJsonReceived.InvokeAsync(new AiJsonGeneratedEventArgs(output.Text!, false));
        }

        if (externalOnOutput.HasDelegate)
        {
            await externalOnOutput.InvokeAsync(output);
        }
    }

    private async Task HandleCompletedAsync(IEnumerable<ResponderOutput> outputs)
    {
        string text = outputs.LastOrDefault(o => o.Type == ResponderOutputType.Text)?.Text
            ?? bufferedOutputs.LastOrDefault(o => o.Type == ResponderOutputType.Text)?.Text
            ?? string.Empty;

        bufferedOutputs.Clear();
        await OnJsonReceived.InvokeAsync(new AiJsonGeneratedEventArgs(text, true));

        if (externalOnOutputEnd.HasDelegate)
        {
            await externalOnOutputEnd.InvokeAsync(outputs);
        }
    }

    private async Task ForwardErrorAsync(string? message)
    {
        bufferedOutputs.Clear();

        if (externalOnError.HasDelegate)
        {
            await externalOnError.InvokeAsync(message);
        }
    }

    private async Task HandleBusyChangedAsync(bool busy)
    {
        if (busy)
        {
            bufferedOutputs.Clear();
        }

        if (externalOnBusyChanged.HasDelegate)
        {
            await externalOnBusyChanged.InvokeAsync(busy);
        }
    }

    public readonly record struct AiJsonGeneratedEventArgs(string Json, bool IsFinal);
}
