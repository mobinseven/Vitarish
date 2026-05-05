using MediatR;
using Vitarish.Clinical.Application.Commands;

namespace Vitarish.Clinical.Interface.Components;

public partial class LinkFhirResourceDialog : CommonBase
{
    private const string LocalEntityType = "Project";

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public Guid PatientProjectId { get; set; }

    private string? _resourceType = "Patient";
    private string? _resourceId;
    private string? _patientReference;
    private string? _errorMessage;
    private bool _saving;

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_resourceType) || string.IsNullOrWhiteSpace(_resourceId))
        {
            _errorMessage = Localizer["ResourceTypeAndIdRequired"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            _ = await Mediator.Send(new CreateResourceLinkCommand(new ResourceLinkWriteDto
            {
                LocalEntityId = PatientProjectId,
                LocalEntityType = LocalEntityType,
                ResourceId = _resourceId,
                ResourceType = _resourceType,
                PatientReference = _patientReference
            }));

            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
