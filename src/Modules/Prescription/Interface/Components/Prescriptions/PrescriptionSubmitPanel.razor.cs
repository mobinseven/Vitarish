namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionSubmitPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;

    private string? trackingCode;
    private Guid? lastPrescriptionId;

    protected override async Task OnParametersSetAsync()
    {
        if (lastPrescriptionId == Prescription?.Id && Prescription?.Status == lastStatus)
        {
            return;
        }

        lastPrescriptionId = Prescription?.Id;
        lastStatus = Prescription?.Status;

        // Tracking code comes from the submission result stored in the domain
        // For now we display status; tracking code is loaded from full prescription detail
        trackingCode = null;
        await Task.CompletedTask;
    }

    private PrescriptionStatus? lastStatus;
}
