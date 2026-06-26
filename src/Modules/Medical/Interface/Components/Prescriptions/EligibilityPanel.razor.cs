using MudBlazor;
namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class EligibilityPanel : PrescriptionBase
{
    [Parameter] public Guid PrescriptionId { get; set; }
    [Parameter] public string? ProviderKey { get; set; }
    [Parameter] public string? PatientNationalId { get; set; }
    [Parameter] public Guid DoctorId { get; set; }
    [Parameter] public Guid ClinicId { get; set; }
    [Parameter] public EventCallback OnEligibilityChecked { get; set; }

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

    private bool IsEligible => eligibility?.Status == EligibilityStatus.Eligible;

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
            eligibility = await MedicalClient.Send(new GetEligibilityQuery
            {
                PatientNationalId = PatientNationalId,
                ProviderKey = ProviderKey,
                DoctorId = DoctorId,
                ClinicId = ClinicId
            });

            // Persist eligibility result on the prescription for submission gating
            if (eligibility is not null)
            {
                _ = await MedicalClient.Send(new RecordEligibilityCommand
                {
                    PrescriptionId = PrescriptionId,
                    Status = eligibility.Status,
                    TrackingCode = eligibility.TrackingCode
                });
            }

            await OnEligibilityChecked.InvokeAsync();
        }
        finally
        {
            isLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
