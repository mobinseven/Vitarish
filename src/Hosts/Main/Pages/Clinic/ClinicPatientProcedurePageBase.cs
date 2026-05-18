using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Main.Pages.Clinic;

public class ClinicPatientProcedurePageBase : ClinicPatientPageBase
{
    [Inject] protected IAccessControlledProceduresService ProcedureService { get; set; } = default!;

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
        Procedure = await ProcedureService.GetAsync(ProcedureId, CancellationToken.None).GetValueOrDefaultAsync();

        if (Procedure is null)
        {
            NavigationManager.NavigateTo(AppRoutes.Clinic.Patient(PatientId));
            return;
        }

        NavService.OverridePageTitle(Procedure.Id.ToString(), Procedure.Name);
        await InvokeAsync(StateHasChanged);
    }
}
