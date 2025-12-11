namespace Vitarish.Interface.Presentation.Components.Account;

public partial class PasswordChangeForm : CommonBase
{
    [Parameter] public string? ReturnUrl { get; set; }
    [Parameter] public string? FailureUrl { get; set; }
    [Parameter] public string? UserId { get; set; } = default!;

    private User User = default!;
    private bool hasPass = false;

    [SupplyParameterFromForm] private ChangePasswordModel ChangePasswordInput { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        if (UserId == null)
        {
            return;
        }

        User = (await UserService.GetUserAsync(UserId))!;
        hasPass = await UserService.UserHasPasswordAsync(UserId);
    }

    private async Task ChangePasswordValidSubmitAsync()
    {
        if (hasPass)
        {
            bool check = await UserService.CheckUserPasswordAsync(UserId, ChangePasswordInput.CurrentPassword!);
            if (!check)
            {
                _ = Snackbar.Add(Localizer["PasswordSetUnsuccessful"], Severity.Info);
                return;
            }
            IdentityResult removePasswordResult = await UserService.RemoveUserPasswordAsync(UserId);
            if (!removePasswordResult.Succeeded)
            {
                _ = Snackbar.Add(Localizer["PasswordSetUnsuccessful"], Severity.Warning);
                return;
            }
        }
        IdentityResult changePasswordResult = await UserService.AddUserPasswordAsync(UserId, ChangePasswordInput.NewPassword!);
        if (!changePasswordResult.Succeeded)
        {
            _ = Snackbar.Add(Localizer["PasswordSetUnsuccessful"], Severity.Error);
            return;
        }
        Dictionary<string, object?> queryParams = new()
        {
            [nameof(User.Id)] = User!.Id,
            [nameof(ReturnUrl)] = WebUtility.UrlEncode(ReturnUrl ?? "/Account/Manage"),
            [nameof(FailureUrl)] = WebUtility.UrlEncode(FailureUrl ?? ""),
        };
        _ = Snackbar.Add(Localizer["PasswordSetSuccessful"], Severity.Success);

        string uri = NavigationManager.GetUriWithQueryParameters("/Account/Manage/PasswordChanged", queryParams);
        NavigationManager.NavigateTo(uri);
    }

    private sealed class ChangePasswordModel
    {
        [Password]
        [Required]
        [DataType(DataType.Password)]
        public string? CurrentPassword { get; set; }

        [Password]
        [Required]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Password]
        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessageResourceName = "ConfirmPasswordError", ErrorMessageResourceType = typeof(Strings))]
        public string? ConfirmPassword { get; set; }
    }

}
