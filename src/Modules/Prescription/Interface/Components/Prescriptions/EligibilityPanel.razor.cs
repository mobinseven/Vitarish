namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class EligibilityPanel : PrescriptionBase
{
    [Parameter] public Guid PrescriptionId { get; set; }
    [Parameter] public string? ProviderKey { get; set; }
    [Parameter] public string? PatientNationalId { get; set; }
    [Parameter] public Guid DoctorId { get; set; }
    [Parameter] public Guid ClinicId { get; set; }

    private EligibilityResultDto? eligibility;
    private bool isLoading;

    private Color EligibilityColor => eligibility?.Status switch
    {
        EligibilityStatus.Eligible => Color.Success,
        EligibilityStatus.NotEligible => Color.Error,
        EligibilityStatus.ProviderUnavailable => Color.Dark,
        EligibilityStatus.RequiresManualVerification => Color.Warning,
        EligibilityStatus.RequiresPatientConfirmation => Color.Warning,
        EligibilityStatus.RequiresFallback => Color.Secondary,
        _ => Color.Default
    };

    private async Task CheckEligibilityAsync()
    {
        if (string.IsNullOrEmpty(ProviderKey) || string.IsNullOrEmpty(PatientNationalId))
        {
            return;
        }

        isLoading = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            eligibility = await Mediator.Send(new GetEligibilityQuery
            {
                PatientNationalId = PatientNationalId,
                ProviderKey = ProviderKey,
                DoctorId = DoctorId,
                ClinicId = ClinicId
            });
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
