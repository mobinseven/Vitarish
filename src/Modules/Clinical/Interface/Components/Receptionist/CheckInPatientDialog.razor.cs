using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Receptionist;

public partial class CheckInPatientDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public string PatientFhirReference { get; set; } = string.Empty;

    private string _visitType = "routine";
    private string? _locationId;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task CheckInAsync()
    {
        if (string.IsNullOrWhiteSpace(_visitType))
        {
            _errorMessage = Localizer["VisitTypeRequired"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            VisitWorkflowReadDto workflow = await Mediator.Send(new CreateVisitWorkflowCommand(
                new VisitWorkflowWriteDto
                {
                    PatientFhirReference = PatientFhirReference,
                    VisitType = _visitType,
                    AssignedLocationId = _locationId
                },
                RequestContext.UserId ?? string.Empty));

            ClinicalActionResult result = await Mediator.Send(new CheckInPatientCommand(
                workflow.Id,
                RequestContext.UserId ?? string.Empty));

            if (result.IsSuccess)
            {
                MudDialog.Close(DialogResult.Ok(workflow.Id));
            }
            else
            {
                _errorMessage = result.ErrorMessage ?? result.ValidationErrors.FirstOrDefault()?.Message;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }
}
