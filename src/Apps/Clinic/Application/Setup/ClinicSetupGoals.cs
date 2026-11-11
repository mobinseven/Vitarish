using MediatR;

namespace Vitarish.Clinic.Application.Setup;

/// <summary>The things a new clinic sets up, in the order the guide works through them.</summary>
public enum ClinicSetupGoal
{
    ServicesAndProviders,
    ShiftsAndBookingRules,
    StaffUsers,
    ClinicProfile
}

public enum ClinicSetupGoalState
{
    Open,
    Done
}

/// <summary>What the clinic has set up so far, as plain facts the policy judges.</summary>
public sealed record ClinicSetupFacts(
    int Services,
    int Providers,
    int ActiveShifts,
    int BookingPolicies,
    int NonOwnerMembers,
    bool HasAddress,
    bool HasLogo,
    bool Dismissed);

public sealed record ClinicSetupGoalProgress(ClinicSetupGoal Goal, ClinicSetupGoalState State);

public sealed record ClinicSetupProgress(
    bool Applies,
    IReadOnlyList<ClinicSetupGoalProgress> Goals,
    ClinicSetupGoal? NextGoal,
    bool Dismissed);

/// <summary>Reads how far the current clinic is with setup.</summary>
public sealed record GetClinicSetupProgressQuery : IRequest<ClinicSetupProgress>;

public enum ClinicSetupGuideDismissal
{
    Updated,
    Forbidden,
    NotFound
}

/// <summary>Sets the current clinic's setup aside for later. Only an owner or platform admin may.</summary>
public sealed record DismissClinicSetupGuideCommand : IRequest<ClinicSetupGuideDismissal>;

/// <summary>Decides each goal's state, their order, the next goal, and whether setup mode applies. Has no I/O.</summary>
public static class ClinicSetupGoals
{
    public static ClinicSetupProgress Evaluate(ClinicSetupFacts facts, bool callerCanManageSetup)
    {
        ClinicSetupGoalProgress[] goals =
        [
            Progress(ClinicSetupGoal.ServicesAndProviders, facts.Services >= 1 && facts.Providers >= 1),
            Progress(ClinicSetupGoal.ShiftsAndBookingRules, facts.ActiveShifts >= 1 && facts.BookingPolicies >= 1),
            Progress(ClinicSetupGoal.StaffUsers, facts.NonOwnerMembers >= 1),
            Progress(ClinicSetupGoal.ClinicProfile, facts.HasAddress && facts.HasLogo)
        ];
        ClinicSetupGoal? next = goals.FirstOrDefault(goal => goal.State == ClinicSetupGoalState.Open)?.Goal;
        bool coreMissing = facts.Services == 0 || facts.Providers == 0 || facts.ActiveShifts == 0;
        return new(callerCanManageSetup && coreMissing && !facts.Dismissed, goals, next, facts.Dismissed);
    }

    /// <summary>What a caller who may not manage setup sees: nothing to set up.</summary>
    public static ClinicSetupProgress NotApplicable { get; } = new(false, [], null, false);

    private static ClinicSetupGoalProgress Progress(ClinicSetupGoal goal, bool done) =>
        new(goal, done ? ClinicSetupGoalState.Done : ClinicSetupGoalState.Open);
}
