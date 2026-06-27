using Microsoft.AspNetCore.Identity;
using Vitarish.Modules.Base.Interface.Common;
using Vitarish.Parties.Client;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Modules.Aggregator.Interface.Components.Admins;

public class AggregatorAdminComponentBase : CommonBase
{
    [Inject] protected IPartiesClient PartiesClient { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected UserManager<User> UserManager { get; set; } = default!;
    [Inject] protected RoleManager<IdentityRole> RoleManager { get; set; } = default!;
}
