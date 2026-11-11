using FluentAssertions;
using Vitarish.Clinic.Application.Setup;
using Xunit;

namespace Clinic.Application.Tests.Setup;

public sealed class ClinicSetupGoalsTests
{
    private static readonly ClinicSetupFacts Empty = new(0, 0, 0, 0, 0, false, false, false);
    private static readonly ClinicSetupFacts Established = new(2, 2, 3, 1, 1, true, true, false);

    [Fact]
    public void An_owner_with_no_services_is_guided_from_the_first_goal()
    {
        ClinicSetupProgress progress = ClinicSetupGoals.Evaluate(Empty, callerCanManageSetup: true);

        progress.Applies.Should().BeTrue();
        progress.Dismissed.Should().BeFalse();
        progress.NextGoal.Should().Be(ClinicSetupGoal.ServicesAndProviders);
        progress.Goals.Select(goal => goal.Goal).Should().Equal(
            ClinicSetupGoal.ServicesAndProviders,
            ClinicSetupGoal.ShiftsAndBookingRules,
            ClinicSetupGoal.StaffUsers,
            ClinicSetupGoal.ClinicProfile);
        progress.Goals.Should().OnlyContain(goal => goal.State == ClinicSetupGoalState.Open);
    }

    [Fact]
    public void An_established_clinic_missing_only_its_logo_is_not_guided()
    {
        ClinicSetupProgress progress = ClinicSetupGoals.Evaluate(Established with { HasLogo = false }, callerCanManageSetup: true);

        progress.Applies.Should().BeFalse();
        progress.NextGoal.Should().Be(ClinicSetupGoal.ClinicProfile);
        progress.Goals.Single(goal => goal.State == ClinicSetupGoalState.Open).Goal.Should().Be(ClinicSetupGoal.ClinicProfile);
    }

    [Fact]
    public void A_clinic_with_every_goal_done_has_no_next_goal()
    {
        ClinicSetupProgress progress = ClinicSetupGoals.Evaluate(Established, callerCanManageSetup: true);

        progress.Applies.Should().BeFalse();
        progress.NextGoal.Should().BeNull();
        progress.Goals.Should().OnlyContain(goal => goal.State == ClinicSetupGoalState.Done);
    }

    [Fact]
    public void Staff_are_never_guided()
    {
        ClinicSetupGoals.Evaluate(Empty, callerCanManageSetup: false).Applies.Should().BeFalse();
    }

    [Fact]
    public void A_dismissed_clinic_is_not_guided_but_reports_the_dismissal()
    {
        ClinicSetupProgress progress = ClinicSetupGoals.Evaluate(Empty with { Dismissed = true }, callerCanManageSetup: true);

        progress.Applies.Should().BeFalse();
        progress.Dismissed.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 1, 1, 0, ClinicSetupGoal.ServicesAndProviders, ClinicSetupGoalState.Done)]
    [InlineData(1, 0, 1, 0, ClinicSetupGoal.ServicesAndProviders, ClinicSetupGoalState.Open)]
    [InlineData(0, 1, 1, 0, ClinicSetupGoal.ServicesAndProviders, ClinicSetupGoalState.Open)]
    [InlineData(1, 1, 1, 1, ClinicSetupGoal.ShiftsAndBookingRules, ClinicSetupGoalState.Done)]
    [InlineData(1, 1, 1, 0, ClinicSetupGoal.ShiftsAndBookingRules, ClinicSetupGoalState.Open)]
    [InlineData(1, 1, 0, 1, ClinicSetupGoal.ShiftsAndBookingRules, ClinicSetupGoalState.Open)]
    public void Each_goal_is_done_only_when_all_its_parts_exist(int services, int providers, int activeShifts, int policies, ClinicSetupGoal goal, ClinicSetupGoalState expected)
    {
        ClinicSetupFacts facts = Empty with { Services = services, Providers = providers, ActiveShifts = activeShifts, BookingPolicies = policies };

        ClinicSetupGoals.Evaluate(facts, callerCanManageSetup: true).Goals.Single(item => item.Goal == goal).State.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, ClinicSetupGoalState.Open)]
    [InlineData(1, ClinicSetupGoalState.Done)]
    public void Staff_users_is_done_with_one_member_besides_owners(int members, ClinicSetupGoalState expected)
    {
        ClinicSetupGoals.Evaluate(Empty with { NonOwnerMembers = members }, callerCanManageSetup: true)
            .Goals.Single(item => item.Goal == ClinicSetupGoal.StaffUsers).State.Should().Be(expected);
    }

    [Theory]
    [InlineData(true, true, ClinicSetupGoalState.Done)]
    [InlineData(true, false, ClinicSetupGoalState.Open)]
    [InlineData(false, true, ClinicSetupGoalState.Open)]
    public void Clinic_profile_needs_both_an_address_and_a_logo(bool address, bool logo, ClinicSetupGoalState expected)
    {
        ClinicSetupGoals.Evaluate(Empty with { HasAddress = address, HasLogo = logo }, callerCanManageSetup: true)
            .Goals.Single(item => item.Goal == ClinicSetupGoal.ClinicProfile).State.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void Setup_applies_while_any_of_services_providers_or_active_shifts_is_missing(int services, int providers, int activeShifts)
    {
        ClinicSetupGoals.Evaluate(Established with { Services = services, Providers = providers, ActiveShifts = activeShifts }, callerCanManageSetup: true)
            .Applies.Should().BeTrue();
    }

    [Fact]
    public void The_next_goal_is_the_first_open_one_in_order()
    {
        ClinicSetupGoals.Evaluate(Established with { NonOwnerMembers = 0, HasLogo = false }, callerCanManageSetup: true)
            .NextGoal.Should().Be(ClinicSetupGoal.StaffUsers);
    }
}
