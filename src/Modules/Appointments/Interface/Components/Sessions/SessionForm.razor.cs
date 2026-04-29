namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionForm : SessionsBase
{
    [Parameter] public SessionReadDto? Session { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IEnumerable<ISessionFormPlugin> FormPlugins { get; set; } = [];
    private ISessionFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private SessionWriteDto sessionWriteDto = new();
    private DateTime? scheduledAtDate;
    private TimeSpan? scheduledAtTime;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Session is not null)
        {
            sessionWriteDto = Session.ToWrite();
        }

        scheduledAtDate = sessionWriteDto.ScheduledAt == default
            ? DateTime.Today
            : sessionWriteDto.ScheduledAt.LocalDateTime.Date;

        scheduledAtTime = sessionWriteDto.ScheduledAt == default
            ? TimeSpan.Zero
            : sessionWriteDto.ScheduledAt.LocalDateTime.TimeOfDay;
    }

    protected virtual async Task SaveAsync()
    {
        DateTimeOffset scheduled = scheduledAtDate.HasValue
            ? new DateTimeOffset(scheduledAtDate.Value.Add(scheduledAtTime ?? TimeSpan.Zero), DateTimeOffset.Now.Offset)
            : sessionWriteDto.ScheduledAt;

        sessionWriteDto = sessionWriteDto with { ScheduledAt = scheduled };

        SessionReadDto saved = Session is null
            ? await SessionsService.AddAsync(sessionWriteDto, CancellationToken.None).EnsureValueAsync()
            : await SessionsService.UpdateAsync(sessionWriteDto, CancellationToken.None).EnsureValueAsync();

        await OnSaved.InvokeAsync(saved.Id);
    }

    protected void Cancel() => OnCancelled.InvokeAsync();
}
