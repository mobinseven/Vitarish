using Vitarish.Sea.Client;
using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicPatientProcedurePageBase : ClinicPatientPageBase
{
    [Inject] protected ISeaClient SeaClient { get; set; } = default!;

    [Parameter] public Guid? procedureId { get; set; }

    protected ProcedureReadDto? Procedure;
    protected Guid ProcedureId;

    protected override async Task ReloadRequired()
    {
        await base.ReloadRequired();

        if (Patient is null)
        {
            return;
        }

        if (procedureId is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patient(PatientId));
            return;
        }

        ProcedureId = procedureId.Value;
        var result = await SeaClient.GetProcedureAsync(ProcedureId, CancellationToken.None);
        Procedure = result.Succeeded ? result.Value : null;

        if (Procedure is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patient(PatientId));
            return;
        }

        NavService.OverridePageTitle(Procedure.Id.ToString(), Procedure.Name);
        await InvokeAsync(StateHasChanged);
    }
}
