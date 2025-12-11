using Vitarish.Parties.Application.Abstractions;

namespace Vitarish.Interface.Presentation.Components.Base
{
    public class AdminBase : CommonBase
    {
        [Inject] protected IUserAccessService UserAccessService { get; set; } = default!;
        [Inject] protected IPartiesService PartiesService { get; set; } = default!;
    }
}
