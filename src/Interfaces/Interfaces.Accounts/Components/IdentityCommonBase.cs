using Duende.IdentityServer.Extensions;
using Vitarish.Modules.Aggregator.Interface.Common;

namespace Vitarish.Interfaces.Accounts.Components
{
    public class IdentityCommonBase : CommonBase
    {
        [Inject] protected IUserService UserService { get; set; } = default!;
        [Inject] protected SignInManager<User> SignInManager { get; set; } = default!;
        [Inject] protected ReturnUrlManager ReturnUrlManager { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService IdentityServerInteractionService { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService InteractionService { get; set; } = default!;
        [Inject] protected IEmailAuthenticationService EmailAuthenticationService { get; set; } = default!;
        [Inject] protected ISmsAuthenticationService SmsAuthenticationService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] protected ILoggerFactory LoggerFactory { get; set; } = default!;
        [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

        protected User? User { get; set; } = default!;

        protected override async Task OnInitializedAsync()
        {
            User = HttpContextAccessor.HttpContext?.User.IsAuthenticated() ?? false ? await UserService.GetUserAsync(HttpContextAccessor.HttpContext.User.GetSubjectId()) : null;
            await ReloadRequired();
            StateHasChanged();
        }
    }
}
