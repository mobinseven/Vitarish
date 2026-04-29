namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionDetail : SessionsBase
{
    [Parameter, EditorRequired] public SessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnReserve { get; set; }

    private SessionReadDto current = default!;

    protected override void OnParametersSet()
    {
        current = Session;
    }
}
