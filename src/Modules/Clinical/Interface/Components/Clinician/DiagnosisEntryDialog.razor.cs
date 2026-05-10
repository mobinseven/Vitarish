using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class DiagnosisEntryDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private string? _code;
    private string _codeSystem = "http://hl7.org/fhir/sid/icd-10";
    private string? _displayText;
    private string _clinicalStatus = "active";
    private string? _notes;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_code) || string.IsNullOrWhiteSpace(_codeSystem))
        {
            _errorMessage = Localizer["DiagnosisCodeRequired"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new AddDiagnosisCommand(
                WorkflowId,
                new DiagnosisPayload
                {
                    Code = _code!,
                    CodeSystem = _codeSystem,
                    DisplayText = _displayText,
                    ClinicalStatus = _clinicalStatus,
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
