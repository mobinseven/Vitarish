namespace Vitarish.Sea.Interface.Components.Base;

public class ProceduresBase : SeaCommonBase
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ProcedureReadDto>();
    }
}
