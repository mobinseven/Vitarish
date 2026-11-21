using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Appointments.Application.Abstractions;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Appointments.Domain.Dto;
using Vitarish.Appointments.Domain.Enums;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Queries.Dashboard;
using Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Operations;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Platform.Idempotency;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.Assistant;

/// <summary>
/// The dashboard shortcut and the chat tool count through the one scheduling capability, so one fixture must give
/// both the same number: active booked appointments only. A temporary hold (even one past its expiry that was
/// not swept yet) and a cancelled, rejected, expired or waitlisted reservation are never counted.
/// </summary>
public sealed class AppointmentCountAgreementTests
{
    private const string PracticeId = "practice-1";
    private static readonly DateOnly Today = new(2026, 11, 21);
    private static readonly DateTimeOffset Now = new(2026, 11, 21, 10, 0, 0, TimeSpan.Zero);

    private static readonly Row[] Fixture =
    [
        new(Today, ReservationStatus.Pending, false),
        new(Today, ReservationStatus.Confirmed, false),
        new(Today, ReservationStatus.Present, false),
        new(Today, ReservationStatus.Completed, false),
        new(Today, ReservationStatus.Pending, true),
        new(Today, ReservationStatus.Confirmed, true),
        new(Today, ReservationStatus.Cancelled, false),
        new(Today, ReservationStatus.Rejected, false),
        new(Today, ReservationStatus.Expired, false),
        new(Today, ReservationStatus.Waitlisted, false),
        new(Today.AddDays(-1), ReservationStatus.Confirmed, false),
        new(Today.AddDays(1), ReservationStatus.Confirmed, false)
    ];

    private const int ExpectedBookedToday = 4;

    [Fact]
    public async Task The_shortcut_counts_only_active_booked_appointments_on_the_local_day()
    {
        TodayAppointmentCountResult result = await new GetTodayAppointmentCountQueryHandler(Capability(), new FixedClock())
            .Handle(new GetTodayAppointmentCountQuery(Context()), CancellationToken.None);

        result.Should().Be(new TodayAppointmentCountResult(TodayAppointmentCountOutcome.Succeeded, Today, ExpectedBookedToday));
    }

    [Fact]
    public async Task The_chat_tool_gives_the_same_count_for_the_same_day()
    {
        CountOperationsAppointmentsTool tool = new(Capability());
        AgentExecutionContext execution = new("run-1", new Dictionary<string, string>
        {
            ["practiceId"] = PracticeId,
            ["authenticatedUserId"] = "user-1"
        });
        string day = Today.ToString("yyyy-MM-dd");

        AgentToolResult result = await tool.ExecuteAsync(new AgentToolCall("call-1", tool.Definition.Name, $$"""{"dateFrom":"{{day}}","dateTo":"{{day}}"}"""), execution);

        using JsonDocument output = JsonDocument.Parse(result.Output);
        output.RootElement.EnumerateObject().Single(property => string.Equals(property.Name, "value", StringComparison.OrdinalIgnoreCase)).Value.GetInt32().Should().Be(ExpectedBookedToday);
    }

    [Fact]
    public async Task A_range_includes_both_boundary_days_and_still_excludes_holds_and_ended_reservations()
    {
        AgentSchedulingResult<int> count = await Capability().CountAppointmentsAsync(
            new(PracticeId, "run", "call", "count"), new(DateFrom: Today.AddDays(-1), DateTo: Today.AddDays(1)), CancellationToken.None);

        count.Value.Should().Be(ExpectedBookedToday + 2);
    }

    [Fact]
    public async Task A_requested_status_outside_the_booked_set_counts_nothing()
    {
        AgentSchedulingResult<int> cancelled = await Capability().CountAppointmentsAsync(
            new(PracticeId, "run", "call", "count"), new(Status: ReservationStatus.Cancelled, DateFrom: Today, DateTo: Today), CancellationToken.None);
        AgentSchedulingResult<int> confirmed = await Capability().CountAppointmentsAsync(
            new(PracticeId, "run", "call", "count"), new(Status: ReservationStatus.Confirmed, DateFrom: Today, DateTo: Today), CancellationToken.None);

        cancelled.Value.Should().Be(0);
        confirmed.Value.Should().Be(1, "the confirmed hold is not a booked appointment");
    }

    private static AppointmentSchedulingAgentCapability Capability()
    {
        Mock<IAccessControlledReservationManager> reservations = new();
        reservations.Setup(item => item.CountAsync(It.IsAny<ReservationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReservationDescriptorDto descriptor, CancellationToken _) => EntityResult<int>.Success(Fixture.Count(row =>
                (descriptor.Status is null || row.Status == descriptor.Status) &&
                (descriptor.IsHold is null || row.IsHold == descriptor.IsHold) &&
                (descriptor.DateFrom is null || row.Date >= descriptor.DateFrom) &&
                (descriptor.DateTo is null || row.Date <= descriptor.DateTo))));
        return new(
            reservations.Object,
            Mock.Of<IAccessControlledShiftManager>(),
            Mock.Of<IShiftBookableDateQueryService>(),
            Mock.Of<IServiceManager>(),
            Mock.Of<IServiceProviderManager>(),
            Mock.Of<IServiceTypeManager>(),
            new PracticeScope(),
            Mock.Of<IOperationReceiptStore<AgentSchedulingProposal>>(),
            Mock.Of<IOperationReceiptStore<AgentSchedulingReceipt>>(),
            new FixedClock());
    }

    private static AssistantContext Context() => new(
        PracticeId, Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner",
        "en-US", "UTC", "conversation", SubjectKind: AssistantSubjects.PracticeKind);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed record Row(DateOnly Date, ReservationStatus Status, bool IsHold);

    private sealed class PracticeScope : IAgentSchedulingContextScope
    {
        public bool IsAllowed(string practiceId) => practiceId == PracticeId;
    }
}
