namespace Vitarish.Intelligence.Interface.Abstractions;

/// <summary>
/// Represents a streaming responder that receives incremental outputs from the AI responder service.
/// </summary>
public interface IStreamResponder
{
    /// <summary>
    /// Gets a value indicating whether the responder is actively responding.
    /// </summary>
    bool Responding { get; }

    /// <summary>
    /// Gets the list of outputs produced so far by the responder.
    /// </summary>
    IReadOnlyList<ResponderOutput> Outputs { get; }

    /// <summary>
    /// Raised when a new output is received from the responder.
    /// </summary>
    event EventHandler<ResponderOutput>? OutputReceived;

    /// <summary>
    /// Raised when the responder has completed producing outputs for the current request.
    /// </summary>
    event EventHandler<IReadOnlyList<ResponderOutput>>? OutputCompleted;

    /// <summary>
    /// Submits inputs to the responder and begins receiving streaming outputs.
    /// </summary>
    /// <param name="inputs">The inputs to submit.</param>
    /// <param name="options">Options that control responder behaviour.</param>
    /// <param name="ct">Cancellation token used to cancel the operation.</param>
    Task Submit(IEnumerable<ResponderInput> inputs, ResponderOptions options, CancellationToken ct = default);
}
