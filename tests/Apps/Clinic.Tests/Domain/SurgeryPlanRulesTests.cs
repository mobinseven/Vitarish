using FluentAssertions;
using Vitarish.Clinic.Domain.Surgery;

namespace Vitarish.Clinic.Tests.Domain;

public sealed class SurgeryPlanRulesTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Overlaps_detects_overlapping_durations()
        => SurgeryPlanRules.Overlaps(Start, 60, Start.AddMinutes(30), 60).Should().BeTrue();

    [Fact]
    public void Overlaps_treats_a_touching_end_and_start_as_not_overlapping()
        => SurgeryPlanRules.Overlaps(Start, 60, Start.AddMinutes(60), 30).Should().BeFalse();

    [Fact]
    public void Overlaps_places_a_durationless_surgery_inside_another_window()
        => SurgeryPlanRules.Overlaps(Start, 60, Start.AddMinutes(30), null).Should().BeTrue();

    [Fact]
    public void Overlaps_excludes_a_durationless_surgery_at_another_window_end()
        => SurgeryPlanRules.Overlaps(Start, 60, Start.AddMinutes(60), null).Should().BeFalse();

    [Fact]
    public void Overlaps_matches_two_durationless_surgeries_only_at_the_same_instant()
    {
        SurgeryPlanRules.Overlaps(Start, null, Start, null).Should().BeTrue();
        SurgeryPlanRules.Overlaps(Start, null, Start.AddMinutes(1), null).Should().BeFalse();
    }

    [Fact]
    public void SameLocation_ignores_case_and_repeated_whitespace()
        => SurgeryPlanRules.SameLocation("  OR  2 ", "or 2").Should().BeTrue();

    [Fact]
    public void SameLocation_rejects_different_locations()
        => SurgeryPlanRules.SameLocation("OR 2", "OR 3").Should().BeFalse();

    [Theory]
    [InlineData(null, "or 2")]
    [InlineData("", "or 2")]
    [InlineData("   ", "or 2")]
    [InlineData("or 2", null)]
    [InlineData(null, null)]
    public void SameLocation_never_matches_a_blank_side(string? first, string? second)
        => SurgeryPlanRules.SameLocation(first, second).Should().BeFalse();

    [Fact]
    public void EnsureCompletable_rejects_a_future_start()
    {
        Action action = () => SurgeryPlanRules.EnsureCompletable(Read(SurgeryStatus.Planned, Start.AddHours(1)), Start);

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SurgeryStatus.Cancelled)]
    [InlineData(SurgeryStatus.Completed)]
    public void EnsureCompletable_rejects_a_non_planned_surgery(SurgeryStatus status)
    {
        Action action = () => SurgeryPlanRules.EnsureCompletable(Read(status, Start.AddHours(-1)), Start);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void EnsureCompletable_allows_a_past_planned_surgery()
    {
        Action action = () => SurgeryPlanRules.EnsureCompletable(Read(SurgeryStatus.Planned, Start.AddHours(-1)), Start);

        action.Should().NotThrow();
    }

    [Fact]
    public void EnsureEditable_allows_a_planned_surgery()
    {
        Action action = () => SurgeryPlanRules.EnsureEditable(Read(SurgeryStatus.Planned, Start.AddHours(-1)));

        action.Should().NotThrow();
    }

    [Theory]
    [InlineData(SurgeryStatus.Cancelled)]
    [InlineData(SurgeryStatus.Completed)]
    public void EnsureEditable_rejects_a_non_planned_surgery(SurgeryStatus status)
    {
        Action action = () => SurgeryPlanRules.EnsureEditable(Read(status, Start.AddHours(-1)));

        action.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SurgeryStatus.Planned)]
    [InlineData(SurgeryStatus.Completed)]
    public void IsCancellable_allows_planned_and_completed_surgeries(SurgeryStatus status)
        => SurgeryPlanRules.IsCancellable(status).Should().BeTrue();

    [Fact]
    public void IsCancellable_rejects_a_cancelled_surgery()
        => SurgeryPlanRules.IsCancellable(SurgeryStatus.Cancelled).Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(1441)]
    public void ValidatePlan_rejects_a_duration_outside_the_supported_range(int durationMinutes)
    {
        Action action = () => SurgeryPlanRules.ValidatePlan(Write(durationMinutes: durationMinutes));

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidatePlan_rejects_a_duplicate_team_user()
    {
        SurgeryWriteModel surgery = Write(team: [new("u1", "One", "Surgeon"), new(" u1 ", "Two", "Nurse")]);

        Action action = () => SurgeryPlanRules.ValidatePlan(surgery);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidatePlan_rejects_a_blank_role()
    {
        SurgeryWriteModel surgery = Write(team: [new("u1", "One", "   ")]);

        Action action = () => SurgeryPlanRules.ValidatePlan(surgery);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ValidatePlan_accepts_a_valid_plan()
    {
        SurgeryWriteModel surgery = Write(team: [new("u1", "One", "Surgeon"), new("u2", "Two", "Nurse")]);

        Action action = () => SurgeryPlanRules.ValidatePlan(surgery);

        action.Should().NotThrow();
    }

    [Fact]
    public void TeamRoles_returns_distinct_trimmed_roles_in_first_seen_order()
    {
        IReadOnlyList<SurgeryTeamMember> team =
        [
            new("u1", "One", "  Surgeon "),
            new("u2", "Two", "Surgeon"),
            new("u3", "Three", "Nurse"),
            new("u4", "Four", "   "),
            new("u5", "Five", "nurse")
        ];

        SurgeryPlanRules.TeamRoles(team).Should().Equal("Surgeon", "Nurse", "nurse");
    }

    private static SurgeryReadModel Read(SurgeryStatus status, DateTimeOffset scheduledAt) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "general-surgery", "Surgery", "key", status, scheduledAt,
            [], [], [], [], Guid.NewGuid(), DateTime.UtcNow, null, null, null, []);

    private static SurgeryWriteModel Write(int? durationMinutes = 30, IReadOnlyList<SurgeryTeamMember>? team = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), "general-surgery", "Surgery", "key", SurgeryStatus.Planned, Start,
            [], [], [], [], Guid.NewGuid(), durationMinutes, "OR 1", team);
}
