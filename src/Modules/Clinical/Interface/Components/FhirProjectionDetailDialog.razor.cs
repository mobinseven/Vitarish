using MediatR;
using Vitarish.Clinical.Application.Commands;

namespace Vitarish.Clinical.Interface.Components;

public partial class FhirProjectionDetailDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    [Parameter, EditorRequired] public ProjectionReadDto Projection { get; set; } = default!;

    private string? _resultMessage;
    private Color _resultColor = Color.Default;
    private bool _syncing;

    private async Task SyncAsync()
    {
        _syncing = true;
        _resultMessage = null;

        try
        {
            ProjectionReadDto result = await Mediator.Send(new SyncProjectionCommand(Projection.ResourceType, Projection.ResourceId));
            _resultMessage = $"{Localizer["SyncedSuccessfully"]}: {result.DisplayName}";
            _resultColor = Color.Success;
        }
        catch (Exception exception)
        {
            _resultMessage = exception.Message;
            _resultColor = Color.Error;
        }
        finally
        {
            _syncing = false;
        }

        await InvokeAsync(StateHasChanged);
    }

    private void Close() => MudDialog.Close(DialogResult.Ok(true));
}
