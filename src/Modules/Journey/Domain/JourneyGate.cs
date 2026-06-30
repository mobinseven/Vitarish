namespace Vitarish.Journey.Domain;

public sealed record JourneyGate(JourneyGateKind Kind, bool IsBlocking, string DestinationUrl)
{
    public static JourneyGate Blocking(JourneyGateKind kind, string destinationUrl) => new(kind, true, destinationUrl);
    public static JourneyGate Prompt(JourneyGateKind kind, string destinationUrl) => new(kind, false, destinationUrl);
}
