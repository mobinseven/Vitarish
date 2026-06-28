using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicPatientPageBase : TenantPageBase
{
    [Inject] protected IAccessControlledProjectsService ProjectsService { get; set; } = default!;

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
        Patient = await ProjectsService.GetAsync(PatientId, CancellationToken.None).GetValueOrDefaultAsync();

        if (Patient is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patients);
            return;
        }

        NavService.OverridePageTitle(Patient.Id.ToString(), Patient.Name);
        await InvokeAsync(StateHasChanged);
    }
}
