using MediatR;
using Vitarish.Clinical.Application.Commands;

namespace Vitarish.Clinical.Interface.Components;

public partial class ResourceLinkDetailDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public ResourceLinkReadDto Link { get; set; } = default!;

    private string? _errorMessage;
    private bool _unlinking;

    private async Task UnlinkAsync()
    {
        _unlinking = true;
        _errorMessage = null;

        try
        {
            await Mediator.Send(new DeleteResourceLinkCommand(Link.Id));
            MudDialog.Close(DialogResult.Ok(true));
        }
        catch (Exception exception)
        {
            _errorMessage = exception.Message;
        }
        finally
        {
            _unlinking = false;
        }
    }

    private void Close() => MudDialog.Cancel();
}
