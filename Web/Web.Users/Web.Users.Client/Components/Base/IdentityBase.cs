namespace Vitarish.Web.Users.Client.Components.Base;

public class IdentityBase : CommonBase
{
    [CascadingParameter] protected HttpContext? HttpContext { get; set; }
    [SupplyParameterFromQuery] protected string? FailureUrl { get; set; }
    [SupplyParameterFromQuery] protected string? ReturnUrl { get; set; }
    
    protected string? SafeReturnUrl => GetSafeReturnUrl(ReturnUrl);
    
    protected string? GetSafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }
        
        // Only allow relative URLs or local URLs
        if (returnUrl.StartsWith("/") || returnUrl.StartsWith("~/"))
        {
            return returnUrl;
        }
        
        // For absolute URLs, return null (not safe)
        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
        {
            return null;
        }
        
        // Relative path without leading slash
        return returnUrl;
    }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        RedirectManager.HttpContext = HttpContext;
        if(ReturnUrl is null)
        {
            return;
        }
        Duende.IdentityServer.Models.AuthorizationRequest? context = await IdentityServerInteractionService.GetAuthorizationContextAsync(ReturnUrl);
        if(context is null)
        {
            return;
        }
        if(context.Client.ClientName is null)
        {
            return;
        }
    }
}
