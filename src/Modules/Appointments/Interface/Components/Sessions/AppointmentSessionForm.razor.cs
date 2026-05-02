namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class AppointmentSessionForm : AppointmentSessionsBase
{
    [Parameter] public AppointmentSessionReadDto? Session { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private AppointmentSessionWriteDto sessionWriteDto = new();
    private DateTime? sessionAtDate;
    private TimeSpan? sessionAtTime;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Session is not null)
        {
            sessionWriteDto = Session.ToWrite();
        }

        sessionAtDate = sessionWriteDto.DateTime.LocalDateTime.Date;
        sessionAtTime = sessionWriteDto.DateTime.LocalDateTime.TimeOfDay;
    }

    protected virtual async Task SaveAsync()
    {
        DateTimeOffset scheduledAt = sessionAtDate.HasValue
            ? new DateTimeOffset(sessionAtDate.Value.Add(sessionAtTime ?? TimeSpan.Zero), DateTimeOffset.Now.Offset)
            : sessionWriteDto.DateTime;

        sessionWriteDto.DateTime = scheduledAt;

        AppointmentSessionReadDto saved = Session is null
            ? await SessionsService.AddAsync(sessionWriteDto, CancellationToken.None).EnsureValueAsync()
            : await SessionsService.UpdateAsync(sessionWriteDto, CancellationToken.None).EnsureValueAsync();

        await OnSaved.InvokeAsync(saved.Id);
    }

    protected void Cancel() => OnCancelled.InvokeAsync();
}
