namespace Vorn.Ai.Server.Contracts.Transcription;

public sealed record TranscriberOptions
{
    public string? ModelId { get; init; }
}
