namespace Vitarish.Medical.Interface.Components.Prescriptions.Prescriptions;

public partial class PrescriptionTimeline : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
}
