using Vitarish.Users.Application.Commands;

namespace Vitarish.Accounts.Interface.Components.Account;

public partial class ProfileEditor
{
    [Parameter] public string Username { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

    private MudForm? _form;
    private bool _isValid;
    private bool _isBusy;

    private User User = default!;

    protected override async Task OnInitializedAsync() => User = (await UserService.GetUserByNameAsync(Username))!;

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
            await UserService.UpdateUserAsync(new UpdateUserCommand(User));
            if (ReturnUrl is null)
            {
                Snackbar.Add(Localizer["ProfileUpdated"]);
            }
            else
            {
                RedirectManager.RedirectTo(WebUtility.UrlDecode(ReturnUrl));
            }
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void NavigateToEmail() => NavigateToAccount("/Account/Manage/Email");

    private void NavigateToPhone() => NavigateToAccount("/Account/Manage/Phone");

    private void NavigateToPassword() => NavigateToAccount("/Account/Manage/Password");

    private void NavigateToTokens() => NavigateToAccount("/Account/Manage/Tokens");

    private void NavigateToAccount(string path)
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl))
        {
            NavigationManager.NavigateTo($"{path}?ReturnUrl={Uri.EscapeDataString(ReturnUrl)}");
            return;
        }

        NavigationManager.NavigateTo(path);
    }
}
