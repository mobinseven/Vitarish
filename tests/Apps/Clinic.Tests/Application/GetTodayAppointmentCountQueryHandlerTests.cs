using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Queries.Dashboard;

namespace Clinic.Tests.Application;

public sealed class GetTodayAppointmentCountQueryHandlerTests
{
    private const string Tehran = "Asia/Tehran";
    private const string NewYork = "America/New_York";

    [Fact]
    public async Task A_day_without_appointments_reports_zero_for_the_practice_local_date()
    {
        Fixture fixture = new(Utc(2026, 11, 21, 10, 0, 0), count: 0);

        TodayAppointmentCountResult result = await fixture.Handle(Tehran);

        result.Should().Be(new TodayAppointmentCountResult(TodayAppointmentCountOutcome.Succeeded, new DateOnly(2026, 11, 21), 0));
    }

    [Theory]
    // Tehran is UTC+03:30 in November 2026 (no daylight saving).
    [InlineData(2026, 11, 20, 20, 30, 0, 2026, 11, 21)] // 00:00:00 local starts the new day
    [InlineData(2026, 11, 20, 20, 29, 59, 2026, 11, 20)] // 23:59:59 local is still the previous day
    [InlineData(2026, 11, 21, 20, 29, 59, 2026, 11, 21)] // 23:59:59 local is still the same day
    [InlineData(2026, 11, 21, 20, 30, 0, 2026, 11, 22)] // next local midnight belongs to the next day
    public async Task The_interval_is_local_midnight_up_to_the_next_local_midnight(
        int year, int month, int day, int hour, int minute, int second, int expectedYear, int expectedMonth, int expectedDay)
    {
        Fixture fixture = new(Utc(year, month, day, hour, minute, second), count: 3);

        TodayAppointmentCountResult result = await fixture.Handle(Tehran);

        DateOnly expected = new(expectedYear, expectedMonth, expectedDay);
        result.Date.Should().Be(expected);
        result.Count.Should().Be(3);
        fixture.Request!.DateFrom.Should().Be(expected);
        fixture.Request.DateTo.Should().Be(expected);
        fixture.Request.Status.Should().BeNull();
    }

    [Theory]
    // New York ends daylight saving on 2026-11-01 at 02:00 local, so that local day lasts 25 hours:
    // 00:00 EDT is 04:00 UTC and the next local midnight (EST) is 05:00 UTC on 2026-11-02.
    [InlineData(2026, 11, 1, 4, 0, 0, 2026, 11, 1)]
    [InlineData(2026, 11, 2, 4, 59, 59, 2026, 11, 1)]
    [InlineData(2026, 11, 2, 5, 0, 0, 2026, 11, 2)]
    [InlineData(2026, 11, 1, 3, 59, 59, 2026, 10, 31)]
    public async Task The_zones_own_rules_decide_the_date_on_a_daylight_saving_day(
        int year, int month, int day, int hour, int minute, int second, int expectedYear, int expectedMonth, int expectedDay)
    {
        Fixture fixture = new(Utc(year, month, day, hour, minute, second), count: 1);

        TodayAppointmentCountResult result = await fixture.Handle(NewYork);

        result.Date.Should().Be(new DateOnly(expectedYear, expectedMonth, expectedDay));
    }

    [Fact]
    public async Task The_count_is_asked_only_for_the_callers_practice_and_user()
    {
        Fixture fixture = new(Utc(2026, 11, 21, 10, 0, 0), count: 2);

        _ = await fixture.Handle(Tehran);

        fixture.Scope!.PracticeId.Should().Be("practice-1");
        fixture.Scope.ActorId.Should().Be("user-1");
        fixture.Scope.AgentRunId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task A_practice_the_capability_does_not_allow_is_forbidden_and_reports_no_count()
    {
        Fixture fixture = new(Utc(2026, 11, 21, 10, 0, 0), count: 9) { Status = AgentSchedulingStatus.ValidationFailure };

        TodayAppointmentCountResult result = await fixture.Handle(Tehran);

        result.Should().Be(new TodayAppointmentCountResult(TodayAppointmentCountOutcome.Forbidden));
    }

    [Fact]
    public async Task A_failed_read_is_unavailable_instead_of_zero()
    {
        Fixture fixture = new(Utc(2026, 11, 21, 10, 0, 0), count: 0) { Status = AgentSchedulingStatus.Failed };

        TodayAppointmentCountResult result = await fixture.Handle(Tehran);

        result.Should().Be(new TodayAppointmentCountResult(TodayAppointmentCountOutcome.Unavailable));
    }

    private static DateTimeOffset Utc(int year, int month, int day, int hour, int minute, int second) =>
        new(year, month, day, hour, minute, second, TimeSpan.Zero);

    private sealed class Fixture(DateTimeOffset now, int count)
    {
        public AgentSchedulingStatus Status { get; init; } = AgentSchedulingStatus.Succeeded;
        public AgentSchedulingContext? Scope { get; private set; }
        public AgentAppointmentListRequest? Request { get; private set; }

        public async Task<TodayAppointmentCountResult> Handle(string timeZone)
        {
            Mock<IAppointmentSchedulingAgentCapability> scheduling = new(MockBehavior.Strict);
            scheduling.Setup(item => item.CountAppointmentsAsync(
                    It.IsAny<AgentSchedulingContext>(), It.IsAny<AgentAppointmentListRequest>(), It.IsAny<CancellationToken>()))
                .Callback<AgentSchedulingContext, AgentAppointmentListRequest, CancellationToken>((scope, request, _) =>
                {
                    Scope = scope;
                    Request = request;
                })
                .ReturnsAsync(new AgentSchedulingResult<int>(Status, count));
            AssistantContext context = new(
                "practice-1", Guid.Empty, "user-1", new HashSet<string>(), "TenantOwner",
                "en-US", timeZone, "conversation", SubjectKind: "practice");

            return await new GetTodayAppointmentCountQueryHandler(scheduling.Object, new FakeTimeProvider(now))
                .Handle(new GetTodayAppointmentCountQuery(context), CancellationToken.None);
        }
    }
}
