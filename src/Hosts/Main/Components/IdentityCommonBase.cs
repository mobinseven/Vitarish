using Duende.IdentityServer.Extensions;
using Vitarish.Modules.Base.Interface.Common;

namespace Vitarish.Main.Components;

public class IdentityCommonBase : CommonBase
{
    [Inject] protected SignInManager<User> SignInManager { get; set; } = default!;
    [Inject] protected ISnackbar Snackbar { get; set; } = default!;
    [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] protected ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    protected User? User { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpContextAccessor.HttpContext?.User.IsAuthenticated() ?? false)
        {
            string userId = HttpContextAccessor.HttpContext.User.GetSubjectId();
            try
            {
                HttpClient client = HttpClientFactory.CreateClient();
                User = await client.GetFromJsonAsync<User>($"/api/modules/users/{userId}");
            }
            catch (Exception ex)
            {
                LoggerFactory.CreateLogger<IdentityCommonBase>().LogError(ex, "Failed to load user profile for {UserId}", userId);
                User = null;
            }
        }
        await ReloadRequired();
        StateHasChanged();
    }
}
