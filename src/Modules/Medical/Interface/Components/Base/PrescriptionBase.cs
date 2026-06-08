using Vitarish.Modules.Base;

namespace Vitarish.Medical.Interface.Components.Base;

public class PrescriptionBase : TenantCommonBase
{
    [Inject] protected IMediator Mediator { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
}
