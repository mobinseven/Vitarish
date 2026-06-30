namespace Vitarish.Journey.Contracts.Responses;

/// <summary>
/// DTO for the complete journey decision response.
/// </summary>
public sealed record JourneyDecisionDto(
    JourneyStepDto Step,
    string? LegacyReturnUrl = null,
    bool? LegacyRequiresProfileCompletion = null
);
