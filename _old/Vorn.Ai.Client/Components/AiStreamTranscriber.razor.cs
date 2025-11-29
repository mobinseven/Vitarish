namespace Vorn.Ai.Client.Components;

using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;
using Vorn.Ai.Client.Abstractions;

public partial class AiStreamTranscriber : ComponentBase, IAsyncDisposable
{
    [Inject] private IStreamTranscriber StreamTranscriber { get; set; } = default!;

    [Parameter] public EventCallback OnTransciptionEnd { get; set; }

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    [Parameter] public EventCallback<string> OnChunk { get; set; }

    [Parameter] public EventCallback<bool> OnRecordingStateChanged { get; set; }

    [Parameter] public EventCallback<bool> OnTranscribingStateChanged { get; set; }

    [Parameter] public EventCallback<bool> OnSupportStatusChanged { get; set; }

    [Parameter]
    public string Value { get; set; } = string.Empty;

    private string CurrentValue => Value ?? string.Empty;

    public bool Supported => StreamTranscriber.Supported;

    public bool Recording => StreamTranscriber.Recording;

    public bool Transcribing => StreamTranscriber.Transcribing;

    protected override void OnInitialized()
    {
        StreamTranscriber.ValueChanged += OnServiceValueChanged;
        StreamTranscriber.ChunkReceived += OnServiceChunkReceived;
        StreamTranscriber.TranscriptionCompleted += OnServiceTranscriptionCompleted;
        StreamTranscriber.RecordingStateChanged += OnServiceRecordingStateChanged;
        StreamTranscriber.TranscribingStateChanged += OnServiceTranscribingStateChanged;
        StreamTranscriber.SupportStatusChanged += OnServiceSupportStatusChanged;
    }

    protected override void OnParametersSet() => Value ??= string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await StreamTranscriber.InitializeAsync();
        }
    }

    public Task ToggleRecordingAsync() => StreamTranscriber.ToggleRecordingAsync();

    public Task StartRecordingAsync() => StreamTranscriber.StartRecordingAsync();

    public ValueTask StopRecordingAsync() => StreamTranscriber.StopRecordingAsync();

    private async void OnServiceValueChanged(object? sender, string newValue) =>
        await InvokeAsync(async () =>
        {
            if (!string.Equals(CurrentValue, newValue, StringComparison.Ordinal))
            {
                Value = newValue;
                await ValueChanged.InvokeAsync(newValue);
            }

            StateHasChanged();
        });

    private async void OnServiceChunkReceived(object? sender, string chunk) =>
        await InvokeAsync(async () =>
        {
            await OnChunk.InvokeAsync(chunk);
        });

    private async void OnServiceTranscriptionCompleted(object? sender, EventArgs args) =>
        await InvokeAsync(async () =>
        {
            await OnTransciptionEnd.InvokeAsync();
            StateHasChanged();
        });

    private async void OnServiceRecordingStateChanged(object? sender, bool recording) =>
        await InvokeAsync(async () =>
        {
            await OnRecordingStateChanged.InvokeAsync(recording);
            StateHasChanged();
        });

    private async void OnServiceTranscribingStateChanged(object? sender, bool transcribing) =>
        await InvokeAsync(async () =>
        {
            await OnTranscribingStateChanged.InvokeAsync(transcribing);
            StateHasChanged();
        });

    private async void OnServiceSupportStatusChanged(object? sender, bool supported) =>
        await InvokeAsync(async () =>
        {
            await OnSupportStatusChanged.InvokeAsync(supported);
            StateHasChanged();
        });

    public async ValueTask DisposeAsync()
    {
        StreamTranscriber.ValueChanged -= OnServiceValueChanged;
        StreamTranscriber.ChunkReceived -= OnServiceChunkReceived;
        StreamTranscriber.TranscriptionCompleted -= OnServiceTranscriptionCompleted;
        StreamTranscriber.RecordingStateChanged -= OnServiceRecordingStateChanged;
        StreamTranscriber.TranscribingStateChanged -= OnServiceTranscribingStateChanged;
        StreamTranscriber.SupportStatusChanged -= OnServiceSupportStatusChanged;
        await StreamTranscriber.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
