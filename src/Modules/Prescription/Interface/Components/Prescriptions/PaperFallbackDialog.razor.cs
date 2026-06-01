namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PaperFallbackDialog : PrescriptionBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public Guid PrescriptionId { get; set; }

    private string? reason;
    private string? paperTrackingNumber;

    private void Cancel() => MudDialog.Cancel();

    private async Task SubmitAsync()
    {
        if (string.IsNullOrWhiteSpace(reason)) return;

        var correlationId = Guid.NewGuid().ToString("N");
        var result = await Mediator.Send(new PaperFallbackCommand
        {
            PrescriptionId = PrescriptionId,
            Reason = reason,
            PaperTrackingNumber = paperTrackingNumber,
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["PaperFallbackSuccess"], Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }
    }
}
