using MediatR;
using Vitarish.Clinical.Application.Commands;

namespace Vitarish.Clinical.Interface.Components;

public partial class CreateResourceLinkDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter] public string? InitialLocalEntityType { get; set; }
    [Parameter] public string? InitialLocalEntityId { get; set; }

    private string? _localEntityType;
    private string? _localEntityId;
    private string? _resourceType;
    private string? _resourceId;
    private string? _patientReference;
    private string? _errorMessage;
    private bool _saving;

    protected override void OnParametersSet()
    {
        _localEntityType = InitialLocalEntityType;
        _localEntityId = InitialLocalEntityId;
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_localEntityType) || string.IsNullOrWhiteSpace(_localEntityId) ||
            string.IsNullOrWhiteSpace(_resourceType) || string.IsNullOrWhiteSpace(_resourceId))
        {
            _errorMessage = Localizer["AllRequiredFieldsMustBeFilled"];
            return;
        }

        if (!Guid.TryParse(_localEntityId, out Guid localEntityGuid))
        {
            _errorMessage = Localizer["InvalidLocalEntityId"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            _ = await Mediator.Send(new CreateResourceLinkCommand(new ResourceLinkWriteDto
            {
                LocalEntityId = localEntityGuid,
                LocalEntityType = _localEntityType,
                PatientReference = _patientReference,
                ResourceId = _resourceId,
                ResourceType = _resourceType
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
