namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemReleaseDetail : MedicalBase
{
    [Parameter, EditorRequired] public CodeSystemRelease Release { get; set; } = default!;
    [Parameter] public EventCallback OnActivationChanged { get; set; }

    private async Task ActivateAsync()
    {
        var result = await Mediator.Send(new ActivateCodeSystemReleaseCommand(Release.Id));
        if (result.IsSuccess)
        {
            await OnActivationChanged.InvokeAsync();
        }
    }

    private async Task DeactivateAsync()
    {
        var result = await Mediator.Send(new DeactivateCodeSystemReleaseCommand(Release.Id));
        if (result.IsSuccess)
        {
            await OnActivationChanged.InvokeAsync();
        }
    }
}
