using MudBlazor;
namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionSubmitPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;

    private string? TrackingCode => Prescription.TrackingCode;
    private IReadOnlyList<OperationalAlertDto>? providerAlerts;
    private bool showOutageWarning;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (!string.IsNullOrEmpty(Prescription.SelectedProviderKey))
        {
            providerAlerts = await MedicalClient.Send(new GetOperationalAlertsQuery
            {
                ProviderKey = Prescription.SelectedProviderKey
            });

            showOutageWarning = providerAlerts?.Any(a =>
                a.AlertType is "ProviderDegraded" or "ProviderOffline") == true;
        }
    }
}
