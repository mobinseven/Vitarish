
namespace Vitarish.Main.Components;

public class TenantPageBase : ComponentBase
{
    [Inject] protected IRequestContext RequestContext { get; set; } = default!;


    [CascadingParameter]
    private Task<AuthenticationState> AuthStateTask { get; set; } = default!;
    protected bool IsAdmin;
    protected bool IsOwner;
    protected Guid? userId;
    protected sealed override async Task OnInitializedAsync()
    {
        ClaimsPrincipal user = (await AuthStateTask).User;
        IsAdmin = user.Identity?.IsAuthenticated == true && user.IsInRole($"{RequestContext.TenantId}:Admin");
        IsOwner = user.Identity?.IsAuthenticated == true && user.IsInRole($"{RequestContext.TenantId}:Owner");
        userId = Guid.Parse(user.GetUserId());
    }
}
