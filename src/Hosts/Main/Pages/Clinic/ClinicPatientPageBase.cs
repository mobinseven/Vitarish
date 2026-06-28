using Vitarish.Sea.Client;
using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicPatientPageBase : TenantPageBase
{
    [Inject] protected ISeaClient SeaClient { get; set; } = default!;

    [Parameter] public Guid? patientId { get; set; }

    protected ProjectReadDto? Patient;
    protected Guid PatientId;

    protected override async Task ReloadRequired()
    {
        if (patientId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patients);
            return;
        }

        PatientId = patientId.Value;
        var result = await SeaClient.GetProjectAsync(PatientId, CancellationToken.None);
        Patient = result.Succeeded ? result.Value : null;

        if (Patient is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patients);
            return;
        }

        NavService.OverridePageTitle(Patient.Id.ToString(), Patient.Name);
        await InvokeAsync(StateHasChanged);
    }
}
