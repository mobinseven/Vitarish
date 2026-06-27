using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Parties.Client;

namespace Vitarish.Parties.Interface.Components.Admins;

public class PartiesAdminComponentBase : CommonBase
{
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
}
