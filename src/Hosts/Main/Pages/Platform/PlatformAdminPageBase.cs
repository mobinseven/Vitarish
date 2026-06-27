using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Parties.Client;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Main.Pages.Platform;

public class PlatformAdminPageBase : CommonBase
{
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected IUsersService UsersService { get; set; } = default!;
}
