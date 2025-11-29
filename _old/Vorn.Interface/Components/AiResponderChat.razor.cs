namespace Vorn.Interface.Components;

using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vorn.Ai.Client.Components;
using Vorn.Ai.Client.Models;
using Vorn.Interface.Common;

public partial class AiResponderChat : CommonBase
{
    private readonly List<AiResponderChatTurn> _turns = [];
    private AiResponderChatTurn? _activeTurn;
    private AiResponderChatTurn? _regenerateTarget;
    private bool _pendingRegeneration;
    private bool _isBusy;
    private bool _isStreaming;
    private string _inputValue = string.Empty;
    private AiResponderChatContext _context = default!;
    private AiResponderChatInput? _chatInput;

    [Parameter]
    public ResponderOptions Options { get; set; } = new();

    [Parameter]
    public string? UserId { get; set; }

    [Parameter]
    public bool FileUploadEnabled { get; set; } = true;

    [Parameter]
    public RenderFragment? LeadingContent { get; set; }

    [Parameter]
    public RenderFragment? TrailingContent { get; set; }

    [Parameter]
    public RenderFragment? PlaceholderContent { get; set; }

    [Parameter]
    public string? InputAreaClass { get; set; }

    [Parameter]
    public string? InitializationPrompt { get; set; }

    [Parameter]
    public RenderFragment<AiResponderChatContext>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? EmptyStateContent { get; set; }

    [Parameter]
    public EventCallback<ResponderOutput> OnOutput { get; set; }
        = EventCallback<ResponderOutput>.Empty;

    [Parameter]
    public EventCallback<IEnumerable<ResponderOutput>> OnOutputEnd { get; set; }
        = EventCallback<IEnumerable<ResponderOutput>>.Empty;

    [Parameter]
    public EventCallback<IReadOnlyList<AiResponderChatTurn>> OnConversationChanged { get; set; }
        = EventCallback<IReadOnlyList<AiResponderChatTurn>>.Empty;

    [Parameter]
    public new EventCallback<string?> OnError { get; set; }
        = EventCallback<string?>.Empty;

    [Parameter]
    public EventCallback<bool> OnBusyChanged { get; set; }
        = EventCallback<bool>.Empty;

    protected override void OnInitialized()
    {
        _context = new AiResponderChatContext(this);
    }

    protected override void OnParametersSet()
    {
        Options ??= new ResponderOptions();
    }

    internal IReadOnlyList<AiResponderChatTurn> Turns => _turns;

    internal bool CanClear => _turns.Count > 0 && !_isBusy;

    internal bool CanRefresh
    {
        get
        {
            if(_turns.Count == 0 || _isBusy)
            {
                return false;
            }

            AiResponderChatTurn last = _turns[^1];
            return !string.IsNullOrWhiteSpace(last.UserText);
        }
    }

    internal bool IsBusy => _isBusy;

    internal bool IsStreaming => _isStreaming;

    internal string InputValue => _inputValue;

    internal RenderFragment ClearButton => builder =>
    {
        builder.OpenComponent<MudFab>(0);
        builder.AddAttribute(1, nameof(MudFab.StartIcon), "fa-thin fa-broom-wide");
        builder.AddAttribute(2, nameof(MudFab.Size), Size.Small);
        builder.AddAttribute(3, nameof(MudFab.Color), Color.Error);
        builder.AddAttribute(4, nameof(MudFab.OnClick), EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, ClearConversationAsync));
        builder.AddAttribute(5, nameof(MudFab.DropShadow), false);
        builder.CloseComponent();
    };

    internal RenderFragment InputFragment => builder =>
    {
        builder.OpenComponent<AiResponderChatInput>(0);
        builder.AddAttribute(1, nameof(AiResponderChatInput.Options), Options);
        builder.AddAttribute(2, nameof(AiResponderChatInput.UserId), UserId);
        builder.AddAttribute(3, nameof(AiResponderChatInput.FileUploadEnabled), FileUploadEnabled);
        builder.AddAttribute(5, nameof(AiResponderChatInput.LeadingContent), ClearButton);
        builder.AddAttribute(6, nameof(AiResponderChatInput.Value), _inputValue);
        builder.AddAttribute(7, nameof(AiResponderChatInput.ValueChanged), EventCallback.Factory.Create<string?>(this, HandleValueChangedAsync));
        builder.AddAttribute(8, nameof(AiResponderChatInput.InitializationPrompt), InitializationPrompt);
        builder.AddAttribute(9, nameof(AiResponderChatInput.Submit), EventCallback.Factory.Create<IReadOnlyList<ResponderInput>>(this, HandleSubmitPayloadAsync));
        builder.AddAttribute(10, nameof(AiResponderChatInput.OnOutput), EventCallback.Factory.Create<ResponderOutput>(this, HandleOutputAsync));
        builder.AddAttribute(11, nameof(AiResponderChatInput.OnOutputEnd), EventCallback.Factory.Create<IEnumerable<ResponderOutput>>(this, HandleOutputEndAsync));
        builder.AddAttribute(12, nameof(AiResponderChatInput.OnError), OnError);
        builder.AddAttribute(13, nameof(AiResponderChatInput.OnBusyChanged), EventCallback.Factory.Create<bool>(this, HandleBusyChangedAsync));
        builder.AddComponentReferenceCapture(15, value => _chatInput = (AiResponderChatInput)value);
        builder.CloseComponent();
    };

    internal async Task SubmitAsync()
    {
        if(_chatInput is not null)
        {
            await _chatInput.SubmitAsync();
        }
    }

    internal async Task ClearAsync()
    {
        if(_isBusy)
        {
            return;
        }

        _turns.Clear();
        _activeTurn = null;
        _regenerateTarget = null;
        _pendingRegeneration = false;
        _isStreaming = false;
        await SetInputValueAsync(string.Empty);

        if(_chatInput is not null)
        {
            await _chatInput.ResetConversationAsync();
        }

        await NotifyConversationChangedAsync();
    }

    internal async Task RefreshAsync()
    {
        if(!CanRefresh)
        {
            return;
        }

        AiResponderChatTurn target = _turns[^1];
        _pendingRegeneration = true;
        _regenerateTarget = target;

        if(_chatInput is not null)
        {
            await _chatInput.SetPreviousResponseIdAsync(target.ParentResponseId);
        }

        await SetInputValueAsync(target.UserText);
        await SubmitAsync();
    }

    internal async Task SetInputValueAsync(string? value)
    {
        string incoming = value ?? string.Empty;
        if(!string.Equals(_inputValue, incoming, StringComparison.Ordinal))
        {
            _inputValue = incoming;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleValueChangedAsync(string? value)
    {
        string incoming = value ?? string.Empty;
        if(!string.Equals(_inputValue, incoming, StringComparison.Ordinal))
        {
            _inputValue = incoming;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HandleSubmitPayloadAsync(IReadOnlyList<ResponderInput> inputs)
    {
        List<ResponderInput> userInputs = MaterializeUserInputs(inputs);
        string? userText = ExtractUserText(userInputs);

        if(_pendingRegeneration && _regenerateTarget is not null)
        {
            _activeTurn = _regenerateTarget;
            _activeTurn.ResetAssistant();
            _activeTurn.SetUserInputs(userInputs, userText ?? string.Empty);
            _regenerateTarget = null;
            _pendingRegeneration = false;
        }
        else
        {
            string? parentResponseId = _chatInput?.PreviousResponseId;
            _activeTurn = new AiResponderChatTurn(userText ?? string.Empty, parentResponseId, userInputs);
            _turns.Add(_activeTurn);
        }

        _activeTurn.IsStreaming = true;

        await SetInputValueAsync(string.Empty);
        await NotifyConversationChangedAsync();
    }

    private async Task HandleOutputAsync(ResponderOutput output)
    {
        if(_activeTurn is null)
        {
            string? parentResponseId = _chatInput?.PreviousResponseId;
            _activeTurn = new AiResponderChatTurn(string.Empty, parentResponseId);
            _turns.Add(_activeTurn);
        }

        _activeTurn.IsStreaming = true;
        _activeTurn.AddOutput(output);

        if(!string.IsNullOrWhiteSpace(output.Id))
        {
            _activeTurn.ResponseId = output.Id;
        }

        _isStreaming = true;

        if(OnOutput.HasDelegate)
        {
            await OnOutput.InvokeAsync(output);
        }

        await NotifyConversationChangedAsync();
    }

    private async Task HandleOutputEndAsync(IEnumerable<ResponderOutput> outputs)
    {
        List<ResponderOutput> materialized = outputs as List<ResponderOutput>
            ?? [.. outputs];

        if(materialized.Count > 0)
        {
            ResponderOutput? lastWithId = materialized.LastOrDefault(o => !string.IsNullOrWhiteSpace(o.Id));
            if(lastWithId is not null && _activeTurn is not null)
            {
                _activeTurn.ResponseId = lastWithId.Id;
            }
        }

        if(_activeTurn is not null)
        {
            foreach(ResponderOutput output in materialized)
            {
                if(!_activeTurn.Outputs.Contains(output))
                {
                    _activeTurn.AddOutput(output);
                }
            }

            _activeTurn.IsStreaming = false;
        }

        _activeTurn = null;
        _isStreaming = false;

        if(OnOutputEnd.HasDelegate)
        {
            await OnOutputEnd.InvokeAsync(materialized);
        }

        await NotifyConversationChangedAsync();
    }

    private async Task HandleBusyChangedAsync(bool busy)
    {
        if(_isBusy != busy)
        {
            _isBusy = busy;

            if(OnBusyChanged.HasDelegate)
            {
                await OnBusyChanged.InvokeAsync(busy);
            }
        }

        if(!busy)
        {
            _isStreaming = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearConversationAsync()
        => await ClearAsync();

    private async Task RefreshLastResponseAsync()
        => await RefreshAsync();

    private async Task NotifyConversationChangedAsync()
    {
        if(OnConversationChanged.HasDelegate)
        {
            IReadOnlyList<AiResponderChatTurn> snapshot = _turns
                .Select(turn => turn.Clone())
                .ToList();

            await OnConversationChanged.InvokeAsync(snapshot);
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ApproveRequestAsync(AiResponderChatTurn turn, ResponderOutput approval)
    {
        if(turn.Approved)
        {
            return;
        }
        await _chatInput!.SubmitPayloadAsync([ResponderInput.McpToolCallApprovalInput(approval.Id!)]);
        turn.Approve();
        await InvokeAsync(StateHasChanged);
    }

    private List<ResponderInput> MaterializeUserInputs(IEnumerable<ResponderInput> inputs)
    {
        List<ResponderInput> materialized = [];

        foreach(ResponderInput input in inputs)
        {
            if(IsInitializationPromptInput(input))
            {
                continue;
            }

            if(input.Type == ResponderInputType.Text && string.IsNullOrWhiteSpace(input.Text))
            {
                continue;
            }

            materialized.Add(input);
        }

        return materialized;
    }

    private string? ExtractUserText(IReadOnlyList<ResponderInput> inputs)
    {
        for(int i = inputs.Count - 1; i >= 0; i--)
        {
            ResponderInput input = inputs[i];
            if(string.IsNullOrWhiteSpace(input.Text))
            {
                continue;
            }

            string text = input.Text!;
            return text;
        }

        return null;
    }

    private bool IsInitializationPromptInput(ResponderInput input)
    {
        if(input.Type != ResponderInputType.Text)
        {
            return false;
        }

        if(string.IsNullOrWhiteSpace(input.Text))
        {
            return false;
        }

        return IsInitializationPromptText(input.Text);
    }

    private string GetFileChipText(ResponderInput input)
    {
        if(!string.IsNullOrWhiteSpace(input.Filename))
        {
            return input.Filename!;
        }

        if(!string.IsNullOrWhiteSpace(input.FileId))
        {
            return input.FileId!;
        }

        if(!string.IsNullOrWhiteSpace(input.FileData))
        {
            return input.FileData!;
        }

        return Localizer["Attachment"];
    }

    private bool IsInitializationPromptText(string text)
    {
        if(string.IsNullOrWhiteSpace(InitializationPrompt))
        {
            return false;
        }

        return string.Equals(text.Trim(), InitializationPrompt.Trim(), StringComparison.Ordinal);
    }

    private static string BuildApprovalExplanationPrompt(ResponderOutput approval)
    {
        StringBuilder builder = new();
        builder.Append("Explain in plain language what will happen if the MCP tool \"");
        builder.Append(approval.ToolName);
        builder.Append("\" is executed");

        if(!string.IsNullOrWhiteSpace(approval.ToolArguments))
        {
            builder.Append(" with the following arguments: ");
            builder.Append(approval.ToolArguments);
            builder.Append('.');
        }
        else
        {
            builder.Append(" without any arguments.");
        }

        builder.Append(" Focus on the practical impact for the user.");

        if(!string.IsNullOrWhiteSpace(approval.ServerLabel))
        {
            builder.Append(' ');
            builder.Append("The tool is hosted on the \"");
            builder.Append(approval.ServerLabel);
            builder.Append("\" server.");
        }

        builder.Append(' ');
        builder.Append("Respond with a concise explanation suitable for end users.");

        return builder.ToString();
    }
    public sealed class AiResponderChatContext
    {
        private readonly AiResponderChat _owner;

        internal AiResponderChatContext(AiResponderChat owner)
            => _owner = owner;

        public IReadOnlyList<AiResponderChatTurn> Turns => _owner.Turns;

        public bool IsBusy => _owner.IsBusy;

        public bool IsStreaming => _owner.IsStreaming;

        public bool CanClear => _owner.CanClear;

        public bool CanRefresh => _owner.CanRefresh;

        public string InputValue
        {
            get => _owner.InputValue;
            set => _ = _owner.SetInputValueAsync(value);
        }

        public RenderFragment Input => _owner.InputFragment;

        public Task SubmitAsync() => _owner.SubmitAsync();

        public Task ClearAsync() => _owner.ClearAsync();

        public Task RefreshAsync() => _owner.RefreshAsync();
    }
}

public sealed class AiResponderChatTurn
{
    private readonly List<ResponderOutput> _outputs = [];
    private readonly List<ResponderInput> _inputs = [];
    private readonly StringBuilder _assistantTextBuilder = new();

    internal AiResponderChatTurn(string userText, string? parentResponseId, IEnumerable<ResponderInput>? inputs = null, DateTimeOffset? createdAt = null)
    {
        UserText = userText;
        ParentResponseId = parentResponseId;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        if(inputs is not null)
        {
            SetUserInputs(inputs, userText);
        }
    }

    public string UserText { get; private set; }

    public string? ParentResponseId { get; }

    public string? ResponseId { get; internal set; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsStreaming { get; internal set; }
    public bool NeedsApproval => _outputs.Any(o => o.Type == ResponderOutputType.McpApprovalRequest);
    public bool Approved { get; internal set; }
    public void Approve() => Approved = true;

    public IReadOnlyList<ResponderOutput> Outputs => _outputs;

    public IReadOnlyList<ResponderInput> Inputs => _inputs;

    public IEnumerable<ResponderInput> TextInputs => _inputs.Where(input => input.Type == ResponderInputType.Text);

    public IEnumerable<ResponderInput> ImageInputs => _inputs.Where(input => input.Type == ResponderInputType.Image);

    public IEnumerable<ResponderInput> FileInputs => _inputs.Where(input => input.Type == ResponderInputType.File);

    public bool HasImageInputs => _inputs.Any(input => input.Type == ResponderInputType.Image);

    public bool HasFileInputs => _inputs.Any(input => input.Type == ResponderInputType.File);

    public string AssistantText => _assistantTextBuilder.ToString();

    public IEnumerable<ResponderOutput> NonTextOutputs => _outputs.Where(o => o.Type != ResponderOutputType.Text);

    internal void AddOutput(ResponderOutput output)
    {
        _outputs.Add(output);
        if(output.Type == ResponderOutputType.Text && !string.IsNullOrWhiteSpace(output.Text))
        {
            _assistantTextBuilder.Clear();
            _assistantTextBuilder.Append(output.Text);
        }
    }

    internal void ResetAssistant()
    {
        _outputs.Clear();
        _assistantTextBuilder.Clear();
        ResponseId = null;
        IsStreaming = true;
    }

    internal void SetUserInputs(IEnumerable<ResponderInput> inputs, string userText)
    {
        _inputs.Clear();
        foreach(ResponderInput input in inputs)
        {
            _inputs.Add(CloneInput(input));
        }

        UserText = userText;
    }

    internal AiResponderChatTurn Clone()
    {
        AiResponderChatTurn clone = new(UserText, ParentResponseId, createdAt: CreatedAt)
        {
            ResponseId = ResponseId,
            IsStreaming = IsStreaming
        };

        foreach(ResponderInput input in _inputs)
        {
            clone._inputs.Add(CloneInput(input));
        }

        foreach(ResponderOutput output in _outputs)
        {
            clone._outputs.Add(output);
            if(output.Type == ResponderOutputType.Text && !string.IsNullOrWhiteSpace(output.Text))
            {
                clone._assistantTextBuilder.Append(output.Text);
            }
        }

        return clone;
    }

    private static ResponderInput CloneInput(ResponderInput input)
    {
        return input.Type switch
        {
            ResponderInputType.Text => ResponderInput.TextInput(input.Text ?? string.Empty),
            ResponderInputType.Image => ResponderInput.ImageInput(input.ImageUrl ?? string.Empty, input.Detail),
            ResponderInputType.File => ResponderInput.FileInput(
                input.FileId ?? string.Empty,
                input.FileData ?? string.Empty,
                input.Filename ?? string.Empty),
            ResponderInputType.McpToolCallApproval => ResponderInput.McpToolCallApprovalInput(input.ApprovalId ?? string.Empty),
            _ => throw new NotSupportedException($"Unsupported responder input type '{input.Type}'.")
        };
    }


}
