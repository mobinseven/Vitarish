namespace Vitarish.Journey.Domain;

/// <summary>
/// Represents a gate in the journey that may block or prompt the user.
/// </summary>
public sealed record JourneyGate(
    JourneyGateKind Kind,
    bool IsBlocking,
    string DestinationUrl,
    string? Message = null
);
