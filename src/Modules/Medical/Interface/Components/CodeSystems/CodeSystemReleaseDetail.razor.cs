namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemReleaseDetail : MedicalBase
{
    [Parameter, EditorRequired] public CodeSystemRelease Release { get; set; } = default!;
    [Parameter] public EventCallback OnActivationChanged { get; set; }

    private async Task ActivateAsync()
    {
        MedicalCommandResult result = await MedicalClient.Send(new ActivateCodeSystemReleaseCommand(Release.Id));
        if (result.IsSuccess)
        {
            await OnActivationChanged.InvokeAsync();
        }
    }

    private async Task DeactivateAsync()
    {
        MedicalCommandResult result = await MedicalClient.Send(new DeactivateCodeSystemReleaseCommand(Release.Id));
        if (result.IsSuccess)
        {
            await OnActivationChanged.InvokeAsync();
        }
    }
}
