namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemDetail : MedicalBase
{
    [Parameter, EditorRequired] public Guid CodeSystemId { get; set; }

    private CodeSystem? codeSystem;
    private List<CodeSystemRelease> releases = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<CodeSystem>();
        RenderForType<CodeSystemRelease>();
    }

    protected override async Task ReloadRequired()
    {
        codeSystem = await QueryService.GetCodeSystemAsync(CodeSystemId);
        releases = [.. await QueryService.ListReleasesAsync(CodeSystemId, activeOnly: false)];
        await InvokeAsync(StateHasChanged);
    }
}
