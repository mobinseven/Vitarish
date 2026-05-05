namespace Vitarish.Clinical.Interface.Components;

public partial class FhirOperationDetailDialog : CommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ClinicalFhirOperationRecord Operation { get; set; } = default!;

    private static string FormatResource(ClinicalFhirOperationRecord operation) =>
        string.IsNullOrWhiteSpace(operation.ResourceId)
            ? operation.ResourceType
            : $"{operation.ResourceType}/{operation.ResourceId}";

    private void Close() => MudDialog.Cancel();
}
