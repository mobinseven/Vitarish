namespace Vitarish.Clients.Interface.Sea.Components.Procedures;


public class ProceduresBase : SeaCommonBase
{
    [Inject] protected IProceduresClient ProceduresService { get; set; } = default!;
    [Inject] protected IReceiptsClient ReceiptsService { get; set; } = default!;
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProcedureDto>();
    }
}
