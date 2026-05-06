using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class MedicationRequestDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private string? _medicationCode;
    private string _medicationCodeSystem = "http://www.nlm.nih.gov/research/umls/rxnorm";
    private string? _medicationDisplay;
    private string? _dosageInstruction;
    private string? _notes;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_medicationCode))
        {
            _errorMessage = Localizer["MedicationCodeRequired"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new AddMedicationRequestCommand(
                WorkflowId,
                new MedicationRequestPayload
                {
                    MedicationCode = _medicationCode!,
                    MedicationCodeSystem = _medicationCodeSystem,
                    MedicationDisplay = _medicationDisplay,
                    DosageInstruction = _dosageInstruction,
                    Notes = _notes
                },
                RequestContext.UserId ?? string.Empty));

            if (result.IsSuccess)
            {
                MudDialog.Close(DialogResult.Ok(true));
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
