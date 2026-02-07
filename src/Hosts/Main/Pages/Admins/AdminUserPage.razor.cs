using Vitarish.Interfaces.Base.Common;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Models;
namespace Vitarish.Main.Pages.Admins;

public partial class AdminUserPage
{
    [Parameter] public string? UserId { get; set; }
    private string password = string.Empty;
    [Inject] private IPasswordHasher<User> PasswordHasher { get; set; } = default!;
    private User? User;
    protected override async Task OnParametersSetAsync()
    {
        if (UserId is not null)
        {
            User = await UsersService.GetUserAsync(UserId);
            NavService.OverridePageTitle(User.Id, User.FullName);
        }
        else
        {
            Back();
        }
    }
    private async void Delete()
    {
        if (User is null)
        {
            return;
        }
        DialogParameters parameters = new()
        {
            { nameof(StrictConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["User"]].Value  },
            { nameof(StrictConfirmDialog.ItemName), User.FullName },
            { nameof(StrictConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(StrictConfirmDialog.Color), Color.Error }
        };

        var dialog = await DialogService.ShowAsync<StrictConfirmDialog>(null, parameters);
        var result = await dialog.Result;

        if(result is null || result.Canceled)
        {
            return;
        }

        IdentityResult res = await UsersService.DeleteUserAsync(new DeleteUserCommand(User.Id));
        _ = res.Succeeded ? Snackbar.Add(Localizer["Deleted"], Severity.Success) : Snackbar.Add(Localizer["Error"], Severity.Warning);
    }
    private async void Save()
    {
        if (User is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            User.PasswordHash = PasswordHasher.HashPassword(User, password);
        }

        IdentityResult userRes = await UsersService.UpdateUserAsync(new UpdateUserCommand(User));
        if (!userRes.Succeeded)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Warning);
            return;
        }

        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
    }

    private void Back() => NavigationManager.NavigateTo("/Admin/Users");

}
