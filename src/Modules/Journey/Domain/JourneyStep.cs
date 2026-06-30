namespace Vitarish.Journey.Domain;

/// <summary>
/// Represents the resolved journey decision.
/// </summary>
public sealed record JourneyStep(
    JourneyDestinationKind DestinationKind,
    string DestinationUrl,
    JourneyGate? BlockingGate = null,
    IReadOnlyList<JourneyGate>? NonBlockingGates = null
)
{
    public bool HasBlockingGate => BlockingGate is not null;
}
