namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionForm : SessionsBase
{
    [Parameter] public SessionReadDto? Session { get; set; }
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    [Inject] private IEnumerable<ISessionFormPlugin> FormPlugins { get; set; } = [];
    private ISessionFormPlugin? Plugin => FormPlugins.SingleOrDefault(p => p.Id == TenantService?.PluginIdentifier);

    private SessionWriteDto sessionWriteDto = new();

    protected override void OnParametersSet()
    {
        if (Session is not null)
        {
            sessionWriteDto = Session.ToWrite();
        }
    }

    private async Task SaveAsync()
    {
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
