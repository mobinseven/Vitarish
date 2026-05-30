namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionSubmitPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;

    private string? TrackingCode => Prescription.TrackingCode;
}
