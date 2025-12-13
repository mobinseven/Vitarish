using Vitarish.Accounts.Interface.Components.Base;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PasswordChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    [Parameter] public string? UserId { get; set; } = default!;

    private User? User { get; set; }
    private bool hasPass;

    private PasswordChangeModel ChangePasswordInput { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (UserId == null)
        {
            return;
        }

        User = await UserService.GetUserAsync(UserId);
        hasPass = await UserService.UserHasPasswordAsync(UserId);
    }
}
