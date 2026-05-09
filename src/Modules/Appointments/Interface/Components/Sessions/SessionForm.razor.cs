namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionForm : SessionsBase
{
    [Parameter] public SessionReadDto? Session { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IEnumerable<ISessionFormPlugin> FormPlugins { get; set; } = [];
    private ISessionFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private SessionWriteDto sessionWriteDto = new();
    private TimeSpan? startTime;
    private TimeSpan? endTime;

    protected override void OnParametersSet()
    {
        if (Session is not null)
        {
            sessionWriteDto = Session.ToWrite();
        }

        sessionWriteDto.TimeZone = string.IsNullOrWhiteSpace(sessionWriteDto.TimeZone)
            ? TimeZoneInfo.Local.Id
            : sessionWriteDto.TimeZone;

        startTime = sessionWriteDto.StartTime.ToTimeSpan();
        endTime = sessionWriteDto.EndTime.ToTimeSpan();
    }

    private async Task SaveAsync()
    {
        sessionWriteDto.StartTime = TimeOnly.FromTimeSpan(startTime ?? sessionWriteDto.StartTime.ToTimeSpan());
        sessionWriteDto.EndTime = TimeOnly.FromTimeSpan(endTime ?? sessionWriteDto.EndTime.ToTimeSpan());

        EntityResult<SessionReadDto> result = sessionWriteDto.Id == Guid.Empty
            ? await SessionManager.AddAsync(sessionWriteDto, CancellationToken.None)
            : await SessionManager.UpdateAsync(sessionWriteDto, CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            await OnSaved.InvokeAsync(result.Value.Id);
        }
    }

    private async Task Cancel() => await OnCancelled.InvokeAsync();

}
