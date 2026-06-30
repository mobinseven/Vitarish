namespace Vitarish.Journey.Contracts.Responses;

/// <summary>
/// DTO for journey step.
/// </summary>
public sealed record JourneyStepDto(
    string DestinationUrl,
    JourneyDestinationKindDto DestinationKind,
    JourneyGateDto? BlockingGate = null,
    IReadOnlyList<JourneyGateDto>? NonBlockingGates = null
);
