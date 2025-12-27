namespace Vitarish.Accounts.Interface.Components.Account;

public partial class PasswordChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    [Parameter] public string? UserId { get; set; } = default!;

    private bool hasPass;

    private PasswordChangeModel ChangePasswordInput { get; set; } = new();

    protected override async Task OnInitializedAsync() => hasPass = await UserService.UserHasPasswordAsync();
}
