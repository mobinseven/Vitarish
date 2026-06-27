using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Medical.Interface.Components.Base;

public class MedicalBase : TenantCommonBase
{
    [Inject] protected IMedicalClient MedicalClient { get; set; } = default!;
}
