using System.Text.Json;
using FluentAssertions;
using MediatR;
using Moq;
using Vitarish.Clinic.Agents;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Application.Setup;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Setup;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Domain.Profiles;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

public sealed class SetupAssistantCapabilityTests
{
    private const string StaffRole = "TenantUser";
    private const string PatientRole = "Patient";

    [Fact]
    public void Setup_capability_belongs_to_the_operations_profile_only()
    {
        AssistantCapabilityKeys.OperationsProfile.Should().Contain(AssistantCapabilityKeys.Setup);
        AssistantCapabilityKeys.Patient.Should().NotContain(AssistantCapabilityKeys.Setup);
        AssistantCapabilityKeys.Setup.Should().Be(NewCapability(new Mock<ISender>().Object).Name);
    }

    [Theory]
    [InlineData(AssistantSubjects.PracticeKind, AssistantOperationsScope.TenantOwnerRole, true)]
    [InlineData(AssistantSubjects.PracticeKind, AssistantOperationsScope.PlatformAdminRole, true)]
    [InlineData(AssistantSubjects.PracticeKind, StaffRole, false)]
    [InlineData(AssistantSubjects.PracticeKind, PatientRole, false)]
    [InlineData(AssistantSubjects.PatientKind, AssistantOperationsScope.TenantOwnerRole, false)]
    [InlineData(AssistantSubjects.PatientKind, PatientRole, false)]
    public void Setup_is_available_only_to_owners_and_admins_on_the_practice_subject(string subjectKind, string role, bool expected)
    {
        NewCapability(new Mock<ISender>().Object).IsAvailable(Context(role, subjectKind)).Should().Be(expected);
    }

    [Fact]
    public async Task Progress_tool_returns_camel_case_goals_next_goal_and_working_hours_as_HH_mm()
    {
        Mock<ISender> sender = SenderReturning(
            new ClinicSetupProgress(
                true,
                [
                    new(ClinicSetupGoal.ServicesAndProviders, ClinicSetupGoalState.Done),
                    new(ClinicSetupGoal.ShiftsAndBookingRules, ClinicSetupGoalState.Open),
                ],
                ClinicSetupGoal.ShiftsAndBookingRules,
                false),
            Profile(
                new TenantWorkingDay(DayOfWeek.Saturday, [new(new TimeOnly(9, 5), new TimeOnly(13, 0)), new(new TimeOnly(16, 0), new TimeOnly(20, 30))]),
                new TenantWorkingDay(DayOfWeek.Monday, [new(new TimeOnly(8, 0), new TimeOnly(12, 0))])));

        JsonElement root = await Run(sender);

        root.GetProperty("goals").EnumerateArray().Select(goal => (goal.GetProperty("goal").GetString(), goal.GetProperty("done").GetBoolean()))
            .Should().Equal(("ServicesAndProviders", true), ("ShiftsAndBookingRules", false));
        root.GetProperty("nextGoal").GetString().Should().Be("ShiftsAndBookingRules");
        JsonElement[] days = root.GetProperty("workingHours").EnumerateArray().ToArray();
        days.Select(day => day.GetProperty("day").GetString()).Should().BeEquivalentTo("Monday", "Saturday");
        JsonElement saturday = days.Single(day => day.GetProperty("day").GetString() == nameof(DayOfWeek.Saturday));
        saturday.GetProperty("intervals").EnumerateArray()
            .Select(interval => (interval.GetProperty("opens").GetString(), interval.GetProperty("closes").GetString()))
            .Should().Equal(("09:05", "13:00"), ("16:00", "20:30"));
        root.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo("goals", "nextGoal", "workingHours");
    }

    [Fact]
    public async Task Progress_tool_returns_an_empty_working_hours_list_when_the_profile_is_missing()
    {
        Mock<ISender> sender = SenderReturning(
            new ClinicSetupProgress(true, [new(ClinicSetupGoal.ClinicProfile, ClinicSetupGoalState.Done)], null, false),
            profile: null);

        JsonElement root = await Run(sender);

        root.GetProperty("workingHours").ValueKind.Should().Be(JsonValueKind.Array);
        root.GetProperty("workingHours").GetArrayLength().Should().Be(0);
        root.GetProperty("nextGoal").ValueKind.Should().Be(JsonValueKind.Null);
        root.GetProperty("goals").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Progress_tool_returns_an_empty_working_hours_list_when_the_profile_has_no_details()
    {
        Mock<ISender> sender = SenderReturning(
            new ClinicSetupProgress(true, [], ClinicSetupGoal.ServicesAndProviders, false),
            new TenantBusinessProfileView("Clinic", null, null, null, null, false));

        JsonElement root = await Run(sender);

        root.GetProperty("workingHours").GetArrayLength().Should().Be(0);
    }

    private static async Task<JsonElement> Run(Mock<ISender> sender)
    {
        GetClinicSetupProgressTool tool = new(sender.Object);

        AgentToolResult result = await tool.ExecuteAsync(
            new AgentToolCall("call-1", GetClinicSetupProgressTool.ToolName, "{}"), new AgentExecutionContext("run-1"), CancellationToken.None);

        result.CallId.Should().Be("call-1");
        result.IsError.Should().BeFalse();
        return JsonDocument.Parse(result.Output).RootElement;
    }

    private static Mock<ISender> SenderReturning(ClinicSetupProgress progress, TenantBusinessProfileView? profile)
    {
        Mock<ISender> sender = new(MockBehavior.Strict);
        sender.Setup(item => item.Send(It.IsAny<GetClinicSetupProgressQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(progress);
        sender.Setup(item => item.Send(It.IsAny<GetTenantBusinessProfileQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        return sender;
    }

    private static TenantBusinessProfileView Profile(params TenantWorkingDay[] workingHours)
    {
        TenantBusinessDetails? details = TenantBusinessDetails.TryCreate(null, null, null, null, workingHours, null, out string? error);
        error.Should().BeNull();
        return new("Clinic", null, null, null, details, false);
    }

    private static SetupAssistantCapability NewCapability(ISender sender) => new(new GetClinicSetupProgressTool(sender));

    private static AssistantContext Context(string role, string subjectKind) =>
        new("practice-1", Guid.Empty, "user-1", new HashSet<string>(StringComparer.Ordinal), role,
            "en-US", "UTC", "conversation-1", SubjectKind: subjectKind);
}
