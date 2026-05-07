namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionForm : SessionsBase
{
    [Parameter] public SessionReadDto? Session { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IEnumerable<ISessionFormPlugin> FormPlugins { get; set; } = [];
    private ISessionFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private SessionWriteDto sessionWriteDto = new();
    private DateTime? startDate;
    private TimeSpan? startTime;
    private DateTime? endDate;
    private TimeSpan? endTime;
    private IReadOnlyList<SessionReminderPolicyWriteDto> reminderPolicies = [];
    private bool reminderPoliciesReady;
    private string? errorMessage;

    protected override void OnParametersSet()
    {
        if (Session is not null)
        {
            sessionWriteDto = Session.ToWrite();
        }

        sessionWriteDto.TimeZone = string.IsNullOrWhiteSpace(sessionWriteDto.TimeZone)
            ? TimeZoneInfo.Local.Id
            : sessionWriteDto.TimeZone;

        startDate = sessionWriteDto.StartAt?.LocalDateTime.Date;
        startTime = sessionWriteDto.StartAt?.LocalDateTime.TimeOfDay;
        endDate = sessionWriteDto.EndAt?.LocalDateTime.Date;
        endTime = sessionWriteDto.EndAt?.LocalDateTime.TimeOfDay;
    }

    private async Task SaveAsync()
    {
        errorMessage = null;
        sessionWriteDto.StartAt = ComposeDateTimeOffset(startDate, startTime, sessionWriteDto.TimeZone);
        sessionWriteDto.EndAt = ComposeDateTimeOffset(endDate, endTime, sessionWriteDto.TimeZone);

        EntityResult<SessionReadDto> result = sessionWriteDto.Id == Guid.Empty
            ? await SessionManager.AddAsync(sessionWriteDto, CancellationToken.None)
            : await SessionManager.UpdateAsync(sessionWriteDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            if (AdminMode && (sessionWriteDto.Id == Guid.Empty || reminderPoliciesReady))
            {
                EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>> reminderResult = await AppointmentReminderService.SaveSessionReminderPoliciesAsync(
                    result.Value.Id,
                    reminderPolicies,
                    CancellationToken.None);
                if (reminderResult.IsFailure)
                {
                    errorMessage = FormatErrors(reminderResult.Errors);
                    return;
                }
            }

            await OnSaved.InvokeAsync(result.Value.Id);
        }
    }

    private static string FormatErrors(IEnumerable<EntityError> errors)
        => string.Join(Environment.NewLine, errors.Select(static error => error.Message));

    private Task SetReminderPoliciesAsync(IReadOnlyList<SessionReminderPolicyWriteDto> policies)
    {
        reminderPolicies = policies;
        reminderPoliciesReady = true;
        return Task.CompletedTask;
    }

    private async Task Cancel() => await OnCancelled.InvokeAsync();

    private static DateTimeOffset? ComposeDateTimeOffset(DateTime? date, TimeSpan? time, string? timeZoneId)
    {
        if (date is null || time is null)
        {
            return null;
        }

        TimeZoneInfo zone;
        try
        {
            zone = !string.IsNullOrWhiteSpace(timeZoneId)
                ? TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)
                : TimeZoneInfo.Local;
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            zone = TimeZoneInfo.Local;
        }

        DateTime localDateTime = DateTime.SpecifyKind(date.Value.Date + time.Value, DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, zone.GetUtcOffset(localDateTime));
    }
}
