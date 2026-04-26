namespace Vitarish.Sea.Interface.Components.Base;


public class ProceduresBase : SeaCommonBase
{
    [Inject] protected IAccessControlledEventsService EventsService { get; set; } = default!;
    [Inject] protected IAccessControlledProceduresService ProceduresService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProcedureReadDto>();
    }
}
