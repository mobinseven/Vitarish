namespace Vitarish.Clinical.Interface.Components;

public partial class AuditEntryDetailDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public AuditLogReadDto Entry { get; set; } = default!;

    private void Close() => MudDialog.Cancel();
}
