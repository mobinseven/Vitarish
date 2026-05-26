namespace Vitarish.Medical.Interface.Components.Valuations;

public partial class ServiceValuationDetail : MedicalBase
{
    [Parameter, EditorRequired] public ServiceValuation Valuation { get; set; } = default!;
}
