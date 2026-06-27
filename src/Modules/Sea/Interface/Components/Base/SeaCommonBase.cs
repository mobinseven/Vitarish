using Vitarish.Modules.Base.Interface;

namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : TenantCommonBase, IDisposable
{
    [Inject] protected ISeaClient SeaClient { get; set; } = default!;
}
