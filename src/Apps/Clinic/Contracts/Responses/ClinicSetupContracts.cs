using System.Text.Json.Serialization;

namespace Vitarish.Clinic.Contracts.Responses;

/// <summary>The things a new clinic sets up, in the order the guide works through them.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ClinicSetupGoal>))]
public enum ClinicSetupGoal
{
    ServicesAndProviders,
    ShiftsAndBookingRules,
    StaffUsers,
    ClinicProfile
}

public sealed record ClinicSetupGoalStateResponse(ClinicSetupGoal Goal, bool Done);

/// <param name="Applies">True while the signed-in owner or platform admin should be guided through setup.</param>
/// <param name="NextGoal">The first goal not yet done; null when all are done.</param>
/// <param name="Dismissed">True once the clinic chose to set up later.</param>
public sealed record ClinicSetupProgressResponse(
    bool Applies,
    IReadOnlyList<ClinicSetupGoalStateResponse> Goals,
    ClinicSetupGoal? NextGoal,
    bool Dismissed);
