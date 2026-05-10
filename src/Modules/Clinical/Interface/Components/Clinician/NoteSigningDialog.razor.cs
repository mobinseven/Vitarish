using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class NoteSigningDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }
    [Parameter, EditorRequired] public string NoteDocumentRef { get; set; } = string.Empty;

    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SignAsync()
    {
        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new SignClinicalNoteCommand(
                WorkflowId,
                NoteDocumentRef,
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
