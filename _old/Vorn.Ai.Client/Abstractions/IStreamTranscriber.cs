namespace Vorn.Ai.Client.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IStreamTranscriber : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether transcription is supported in the current environment.
    /// </summary>
    bool Supported { get; }

    /// <summary>
    /// Gets a value indicating whether the transcriber is currently recording audio.
    /// </summary>
    bool Recording { get; }

    /// <summary>
    /// Gets a value indicating whether the transcriber is actively transcribing recorded audio.
    /// </summary>
    bool Transcribing { get; }

    /// <summary>
    /// Gets the current transcribed text value.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Raised when the transcribed value changes.
    /// </summary>
    event EventHandler<string>? ValueChanged;

    /// <summary>
    /// Raised when a chunk of transcribed text is received.
    /// </summary>
    event EventHandler<string>? ChunkReceived;

    /// <summary>
    /// Raised when the recording state changes.
    /// </summary>
    event EventHandler<bool>? RecordingStateChanged;

    /// <summary>
    /// Raised when the transcribing state changes.
    /// </summary>
    event EventHandler<bool>? TranscribingStateChanged;

    /// <summary>
    /// Raised when the support status for transcription changes.
    /// </summary>
    event EventHandler<bool>? SupportStatusChanged;

    /// <summary>
    /// Raised when a transcription session completes.
    /// </summary>
    event EventHandler? TranscriptionCompleted;

    /// <summary>
    /// Initializes the transcriber for use.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Toggles recording state on or off.
    /// </summary>
    Task ToggleRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts recording audio.
    /// </summary>
    Task StartRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops recording and returns when cleanup is complete.
    /// </summary>
    ValueTask StopRecordingAsync();
}
