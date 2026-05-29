using Vitarish.Modules.Base;

namespace Vitarish.Prescription.Interface.Components.Base;

public class PrescriptionBase : TenantCommonBase
{
    [Inject] protected IMediator Mediator { get; set; } = default!;
}
