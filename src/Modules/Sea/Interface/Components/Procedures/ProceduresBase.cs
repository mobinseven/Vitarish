namespace Vitarish.Sea.Interface.Components.Procedures;


public class ProceduresBase : SeaCommonBase
{
    [Inject] protected IProceduresService ProceduresService { get; set; } = default!;
    [Inject] protected IReceiptsService ReceiptsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProcedureDto>();
    }
}
