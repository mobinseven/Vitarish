namespace Vitarish.Appointments.Interface.Plugins.Default.Components;

public partial class DefaultAppointmentForm : AppointmentsBase
{
    [Parameter, EditorRequired] public AppointmentWriteDto Appointment { get; set; } = default!;
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private DateTime? scheduledAtDate;
    private TimeSpan? scheduledAtTime;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        scheduledAtDate = Appointment.ScheduledAt.LocalDateTime.Date;
        scheduledAtTime = Appointment.ScheduledAt.LocalDateTime.TimeOfDay;
    }

    private async Task SaveAsync()
    {
        DateTimeOffset scheduled = scheduledAtDate.HasValue
            ? new DateTimeOffset(scheduledAtDate.Value.Add(scheduledAtTime ?? TimeSpan.Zero), DateTimeOffset.Now.Offset)
            : Appointment.ScheduledAt;

        Appointment.ScheduledAt = scheduled;

        AppointmentReadDto saved = Appointment.Id == Guid.Empty
            ? await AppointmentsService.AddAsync(Appointment, CancellationToken.None).EnsureValueAsync()
            : await AppointmentsService.UpdateAsync(Appointment, CancellationToken.None).EnsureValueAsync();

        await OnSaved.InvokeAsync(saved.Id);
    }

    private void Cancel() => OnCancelled.InvokeAsync();
}
