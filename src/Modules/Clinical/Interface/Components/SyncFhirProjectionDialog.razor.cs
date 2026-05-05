using MediatR;
using Vitarish.Clinical.Application.Commands;

namespace Vitarish.Clinical.Interface.Components;

public partial class SyncFhirProjectionDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Inject] private IMediator Mediator { get; set; } = default!;

    private string? _resourceType;
    private string? _resourceId;
    private string? _resultMessage;
    private Color _resultColor = Color.Default;
    private bool _syncing;

    private async Task SyncAsync()
    {
        if (string.IsNullOrWhiteSpace(_resourceType) || string.IsNullOrWhiteSpace(_resourceId))
        {
            _resultMessage = Localizer["ResourceTypeAndIdRequired"];
            _resultColor = Color.Warning;
            return;
        }

        _syncing = true;
        _resultMessage = null;

        try
        {
            ProjectionReadDto result = await Mediator.Send(new SyncProjectionCommand(_resourceType, _resourceId));
            _resultMessage = $"{Localizer["SyncedSuccessfully"]}: {result.DisplayName}";
            _resultColor = Color.Success;
            await InvokeAsync(StateHasChanged);
            MudDialog.Close(DialogResult.Ok(true));
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
    }

    private void Cancel() => MudDialog.Cancel();
}
