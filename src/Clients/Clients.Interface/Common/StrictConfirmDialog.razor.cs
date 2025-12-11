namespace Vitarish.Clients.Interface.Common;


public partial class StrictConfirmDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string? ContentText { get; set; }
    [Parameter] public required string ItemName { get; set; }
    [Parameter] public required string ButtonText { get; set; }
    [Parameter] public Color Color { get; set; } = Color.Error;

    private string? confirmation;
    private bool IsMatch => string.Equals(confirmation, ItemName, StringComparison.OrdinalIgnoreCase);

    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => MudDialog.Cancel();
}
