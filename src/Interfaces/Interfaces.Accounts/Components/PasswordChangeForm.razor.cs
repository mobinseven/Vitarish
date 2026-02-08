namespace Vitarish.Interfaces.Accounts.Components;

public partial class PasswordChangeForm : IdentityCommonBase
{
    [Parameter] public string? UserId { get; set; } = default!;

    private bool hasPass;

    private PasswordChangeModel ChangePasswordInput { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (User is null)
        {
            hasPass = false;
            return;
        }

        hasPass = await UserService.UserHasPasswordAsync(User.Id);
    }
}
