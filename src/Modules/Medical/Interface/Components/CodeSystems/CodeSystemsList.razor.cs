namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemsList : MedicalBase
{
    [Parameter] public EventCallback<Guid> OnClicked { get; set; }

    private List<CodeSystem> codeSystems = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<CodeSystem>();
    }

    protected override async Task ReloadRequired()
    {
        codeSystems = [.. await MedicalClient.ListCodeSystemsAsync(activeOnly: false)];
        await InvokeAsync(StateHasChanged);
    }
}
