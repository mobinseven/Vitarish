using MediatR;
using Vitarish.Appointments.Application.AgentCapabilities;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Presentation;

namespace Vitarish.Clinic.Application.Queries.Dashboard;

public enum TodayAppointmentCountOutcome
{
    Succeeded,
    Forbidden,
    Unavailable
}

public sealed record TodayAppointmentCountResult(TodayAppointmentCountOutcome Outcome, DateOnly Date = default, int Count = 0);

/// <summary>Counts the appointments on the practice-local calendar day that contains the current instant. Read-only.</summary>
public sealed record GetTodayAppointmentCountQuery(AssistantContext Context) : IRequest<TodayAppointmentCountResult>;

/// <summary>
/// Appointments are keyed by their local calendar date, so the interval from local midnight up to (not
/// including) the next local midnight is exactly that one date. The zone's own rules (including a
/// daylight-saving shift) decide which date the current instant falls on.
/// </summary>
public static class PracticeLocalDay
{
    public static DateOnly Containing(DateTimeOffset instant, string timeZoneId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime);
}

/// <summary>Reuses the access-controlled Appointments count; it starts no agent turn, provider request or proposal.</summary>
public sealed class GetTodayAppointmentCountQueryHandler(
    IAppointmentSchedulingAgentCapability scheduling,
    TimeProvider timeProvider) : IRequestHandler<GetTodayAppointmentCountQuery, TodayAppointmentCountResult>
{
    public async Task<TodayAppointmentCountResult> Handle(GetTodayAppointmentCountQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = PracticeLocalDay.Containing(timeProvider.GetUtcNow(), request.Context.TimeZone);
        string callId = Guid.CreateVersion7().ToString("N");
        AgentSchedulingContext scope = new(
            request.Context.PracticeId, callId, callId, ClinicPresentableToolNames.CountOperationsAppointments, request.Context.AuthenticatedUserId);

        AgentSchedulingResult<int> result = await scheduling.CountAppointmentsAsync(scope, new(DateFrom: today, DateTo: today), cancellationToken);
        return result.Status switch
        {
            AgentSchedulingStatus.Succeeded => new(TodayAppointmentCountOutcome.Succeeded, today, result.Value),
            AgentSchedulingStatus.Forbidden or AgentSchedulingStatus.ValidationFailure => new(TodayAppointmentCountOutcome.Forbidden),
            _ => new(TodayAppointmentCountOutcome.Unavailable)
        };
    }
}
