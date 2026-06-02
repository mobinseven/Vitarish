namespace Vitarish.Interfaces.Accounts.Components;

public class IdentityBase : IdentityCommonBase
{
    [CascadingParameter] protected HttpContext? HttpContext { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        string? returnUrl = ReturnUrlManager.GetReturnUrl();

        if (returnUrl is null)
        {
            return;
        }

        Duende.IdentityServer.Models.AuthorizationRequest? context = await IdentityServerInteractionService.GetAuthorizationContextAsync(returnUrl);
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
