namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AppointmentForm : AppointmentsBase
{
    [Parameter] public AppointmentReadDto? Appointment { get; set; }
    [Parameter] public Guid? PreselectedSessionId { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IAccessControlledSessionsService SessionsService { get; set; } = default!;
    [Inject] private IEnumerable<IAppointmentFormPlugin> FormPlugins { get; set; } = [];
    private IAppointmentFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private AppointmentWriteDto appointmentWriteDto = new();
    private List<SessionReadDto> availableSessions = [];
    private SessionReadDto? selectedSession;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Appointment is not null)
        {
            appointmentWriteDto = Appointment.ToWrite();
        }

        if (PreselectedSessionId.HasValue && appointmentWriteDto.SessionId is null)
        {
            appointmentWriteDto = appointmentWriteDto with { SessionId = PreselectedSessionId };
        }
    }

    protected override async Task OnInitializedAsync()
    {
        SessionDescriptorDto descriptor = new()
        {
            Status = SessionStatus.Active,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<SessionReadDto> sessions = await SessionsService
            .GetListAsync(descriptor, CancellationToken.None)
            .GetValueOrDefaultAsync() ?? [];

        availableSessions = [.. sessions.OrderBy(s => s.ScheduledAt)];

        if (appointmentWriteDto.SessionId is { } sessionId)
        {
            selectedSession = availableSessions.FirstOrDefault(s => s.Id == sessionId);
        }
    }

    private void SelectSession(Guid? sessionId)
    {
        appointmentWriteDto = appointmentWriteDto with { SessionId = sessionId };
        selectedSession = sessionId is not null
            ? availableSessions.FirstOrDefault(s => s.Id == sessionId)
            : null;
    }

    protected virtual async Task SaveAsync()
    {
        AppointmentReadDto saved = Appointment is null
            ? await AppointmentsService.AddAsync(appointmentWriteDto, CancellationToken.None).EnsureValueAsync()
            : await AppointmentsService.UpdateAsync(appointmentWriteDto, CancellationToken.None).EnsureValueAsync();

        await OnSaved.InvokeAsync(saved.Id);
    }

    protected void Cancel() => OnCancelled.InvokeAsync();
}
