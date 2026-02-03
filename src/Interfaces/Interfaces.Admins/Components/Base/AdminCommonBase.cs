using Vitarish.Interfaces.Base.Common;
using Vitarish.Parties.Application.Abstractions;

namespace Vitarish.Interfaces.Admins.Components.Base
{
    public class AdminCommonBase : CommonBase
    {
        [Inject] protected IUserStore<User> UserStore { get; set; } = default!;
        [Inject] protected SignInManager<User> SignInManager { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IJSRuntime Js { get; set; } = default!;
        [Inject] protected IRequestContext RequestContext { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService IdentityServerInteractionService { get; set; } = default!;
        [Inject] protected IRequestIdentitiesService RequestIdentitiesService { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService InteractionService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] protected ILoggerFactory LoggerFactory { get; set; } = default!;
        [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
        [Inject] protected IPartiesService PartiesService { get; set; } = default!;
        [Inject] protected IUsersService UsersService { get; set; } = default!;
        [Inject] protected UserManager<User> UserManager { get; set; } = default!;
        [Inject] protected RoleManager<IdentityRole> RoleManager { get; set; } = default!;
    }
}
