using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Interface.Components.Clinician;

public partial class ClinicalNoteDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private IRequestContext RequestContext { get; set; } = default!;

    [Parameter, EditorRequired] public Guid WorkflowId { get; set; }

    private string _noteType = "11488-4";
    private string? _title;
    private string? _content;
    private string? _errorMessage;
    private bool _saving;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_content))
        {
            _errorMessage = Localizer["NoteContentRequired"];
            return;
        }

        _saving = true;
        _errorMessage = null;

        try
        {
            ClinicalActionResult result = await Mediator.Send(new AddClinicalNoteCommand(
                WorkflowId,
                new ClinicalNotePayload
                {
                    NoteType = _noteType,
                    Title = _title,
                    Content = _content!
                },
                RequestContext.UserId ?? string.Empty));

            if (result.IsSuccess)
            {
                MudDialog.Close(DialogResult.Ok(result.FhirRefsCreated.FirstOrDefault()));
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
