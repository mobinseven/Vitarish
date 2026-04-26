namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AppointmentForm : AppointmentsBase
{
    [Parameter] public AppointmentReadDto? Appointment { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IEnumerable<IAppointmentFormPlugin> FormPlugins { get; set; } = [];
    private IAppointmentFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private AppointmentWriteDto appointmentWriteDto = new();
    private DateTime? scheduledAtDate;
    private TimeSpan? scheduledAtTime;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Appointment is not null)
        {
            appointmentWriteDto = Appointment.ToWrite();
        }

        scheduledAtDate = appointmentWriteDto.ScheduledAt.LocalDateTime.Date;
        scheduledAtTime = appointmentWriteDto.ScheduledAt.LocalDateTime.TimeOfDay;
    }

    protected virtual async Task SaveAsync()
    {
        DateTimeOffset scheduled = scheduledAtDate.HasValue
            ? new DateTimeOffset(scheduledAtDate.Value.Add(scheduledAtTime ?? TimeSpan.Zero), DateTimeOffset.Now.Offset)
            : appointmentWriteDto.ScheduledAt;

        appointmentWriteDto.ScheduledAt = scheduled;

        AppointmentReadDto saved = Appointment is null
            ? await AppointmentsService.AddAsync(appointmentWriteDto, CancellationToken.None).EnsureValueAsync()
            : await AppointmentsService.UpdateAsync(appointmentWriteDto, CancellationToken.None).EnsureValueAsync();

        await OnSaved.InvokeAsync(saved.Id);
    }

    protected void Cancel() => OnCancelled.InvokeAsync();
}
