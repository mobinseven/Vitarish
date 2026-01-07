namespace Vitarish.Accounts.Interface.Components.Account;

public partial class ProfileEditor
{
    private MudForm? _form;
    private bool _isValid;
    private bool _isBusy;
    private string? _userTheme => User.ProfilePreferences.ContainsKey("UserTheme") ? User.ProfilePreferences["UserTheme"] : null;
    private async Task UpdateUser()
    {
        if (_form is not null)
        {
            await _form.Validate();
            if (!_form.IsValid)
            {
                return;
            }
        }

        _isBusy = true;
        try
        {
            _ = await UserService.UpdateUserAsync(User.Id, User);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void NavigateToEmail() => NavigationManager.NavigateTo("/Account/Manage/Email");

    private void NavigateToPhone() => NavigationManager.NavigateTo("/Account/Manage/Phone");

    private void NavigateToPassword() => NavigationManager.NavigateTo("/Account/Manage/Password");

    private void NavigateToTokens() => NavigationManager.NavigateTo("/Account/Manage/Tokens");

}
