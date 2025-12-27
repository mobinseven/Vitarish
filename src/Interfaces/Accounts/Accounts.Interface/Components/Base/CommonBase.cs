namespace Vitarish.Accounts.Interface.Components.Base
{
    public class CommonBase : ComponentBase
    {
        [Inject] protected ICipherService CipherService { get; set; } = default!;
        [Inject] protected IUserService UserService { get; set; } = default!;
        [Inject] protected IUserStore<User> UserStore { get; set; } = default!;
        [Inject] protected SignInManager<User> SignInManager { get; set; } = default!;
        [Inject] protected NavigationManager NavigationManager { get; set; } = default!;
        [Inject] protected IStringLocalizer Localizer { get; set; } = default!;
        [Inject] protected IdentityRedirectManager RedirectManager { get; set; } = default!;
        [Inject] protected ISnackbar Snackbar { get; set; } = default!;
        [Inject] protected IJSRuntime Js { get; set; } = default!;
        [Inject] protected IRequestContext RequestContext { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService IdentityServerInteractionService { get; set; } = default!;
        [Inject] protected IApiClientsService ApiClientService { get; set; } = default!;
        [Inject] protected IPersonalAccessTokenService PersonalAccessTokenService { get; set; } = default!;
        [Inject] protected IAuthenticationClientsService AuthenticationClientsService { get; set; } = default!;
        [Inject] protected IRequestIdentitiesService RequestIdentitiesService { get; set; } = default!;
        [Inject] protected IIdentityServerInteractionService InteractionService { get; set; } = default!;
        [Inject] protected IEmailAuthenticationService EmailAuthenticationService { get; set; } = default!;
        [Inject] protected ISmsAuthenticationService SmsAuthenticationService { get; set; } = default!;
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
        [Inject] protected ILoggerFactory LoggerFactory { get; set; } = default!;
    }
}
