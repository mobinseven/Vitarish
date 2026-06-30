namespace Vitarish.Journey.Contracts.Responses;

/// <summary>
/// DTO for journey destination kind.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<JourneyDestinationKindDto>))]
public enum JourneyDestinationKindDto
{
    Gate = 1,
    ReturnUrl = 2,
    AppDashboard = 3,
    UserHome = 4,
    GlobalDashboard = 5
}
