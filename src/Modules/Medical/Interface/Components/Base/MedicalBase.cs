using Vitarish.Modules.Base;

namespace Vitarish.Medical.Interface.Components.Base;

public class MedicalBase : TenantCommonBase
{
    [Inject] protected IMedicalClient MedicalClient { get; set; } = default!;
}
