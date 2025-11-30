
using Vorn.Aaas.Users.Presentation.Abstractions;

namespace Vorn.Aaas.Interface.Presentation.Components.Base
{
    public class AdminBase : CommonBase
    {
        [Inject] protected IUserAccessService UserAccessService { get; set; } = default!;
    }
}
