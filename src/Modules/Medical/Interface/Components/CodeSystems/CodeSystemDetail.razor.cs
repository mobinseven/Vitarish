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
        Task<CodeSystem?> codeSystemTask = MedicalClient.GetCodeSystemAsync(CodeSystemId);
        Task<IReadOnlyList<CodeSystemRelease>> releasesTask = MedicalClient.ListReleasesAsync(CodeSystemId, activeOnly: false);

        await Task.WhenAll(codeSystemTask, releasesTask);

        codeSystem = await codeSystemTask;
        releases = [.. await releasesTask];
        await InvokeAsync(StateHasChanged);
    }
}
