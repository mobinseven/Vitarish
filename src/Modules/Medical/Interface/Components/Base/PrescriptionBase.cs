using MudBlazor;
using Vitarish.Modules.Base.Interface;

namespace Vitarish.Medical.Interface.Components.Base;

public class PrescriptionBase : TenantCommonBase
{
    [Inject] protected IMedicalClient MedicalClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
}
