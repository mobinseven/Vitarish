namespace Vitarish.Journey.Domain;

public sealed record JourneyDecision(JourneyStep Step, IReadOnlyList<JourneyGate> Gates)
{
    public string DestinationUrl => Step.DestinationUrl;
    public JourneyGate? BlockingGate => Gates.FirstOrDefault(gate => gate.IsBlocking);
    public bool IsBlocked => BlockingGate is not null;
}
