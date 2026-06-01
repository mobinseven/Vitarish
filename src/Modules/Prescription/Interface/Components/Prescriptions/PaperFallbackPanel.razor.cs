namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PaperFallbackPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public Guid PrescriptionId { get; set; }
    [Parameter, EditorRequired] public PrescriptionStatus CurrentStatus { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    /// <summary>
    /// Paper fallback is available when the prescription is in Draft, ReadyForSubmission, or ProviderUnavailable.
    /// </summary>
    private bool ShowFallbackOption => CurrentStatus is
        PrescriptionStatus.Draft or
        PrescriptionStatus.ReadyForSubmission or
        PrescriptionStatus.ProviderUnavailable;

    private async Task ShowPaperFallbackDialogAsync()
    {
        var parameters = new DialogParameters
        {
            { nameof(PaperFallbackDialog.PrescriptionId), PrescriptionId }
        };

        var dialog = await DialogService.ShowAsync<PaperFallbackDialog>(
            Localizer["PaperFallback"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is not null && !dialogResult.Canceled)
        {
            await OnStatusChanged.InvokeAsync();
        }
    }
}
