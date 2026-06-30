namespace Vitarish.Journey.Contracts.Responses;

/// <summary>
/// DTO for journey gate.
/// </summary>
public sealed record JourneyGateDto(
    JourneyGateKindDto Kind,
    bool IsBlocking,
    string DestinationUrl,
    string? Message = null
);
