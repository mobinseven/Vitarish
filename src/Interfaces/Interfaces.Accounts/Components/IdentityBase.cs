namespace Vitarish.Interfaces.Accounts.Components;

public class IdentityBase : IdentityCommonBase
{
    [CascadingParameter] protected HttpContext? HttpContext { get; set; }
    [SupplyParameterFromQuery] protected string? FailureUrl { get; set; }
    [SupplyParameterFromQuery] protected string? ReturnUrl { get; set; }
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        RedirectManager.HttpContext = HttpContext;
        if (ReturnUrl is null)
        {
            return;
        }
        Duende.IdentityServer.Models.AuthorizationRequest? context = await IdentityServerInteractionService.GetAuthorizationContextAsync(ReturnUrl);
        if (context is null)
        {
            return;
        }
        if (context.Client.ClientName is null)
        {
            return;
        }
    }
}
