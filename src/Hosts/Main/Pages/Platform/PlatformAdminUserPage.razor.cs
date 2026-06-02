using Vitarish.Interfaces.Admins.Components;
using Vitarish.Interfaces.Base.Common;
using Vitarish.Users.Application.Commands;
namespace Vitarish.Main.Pages.Platform;

public partial class PlatformAdminUserPage : PlatformAdminCommonBase
{
    [Parameter] public Guid? UserId { get; set; }
    private string password = string.Empty;
    [Inject] private IPasswordHasher<User> PasswordHasher { get; set; } = default!;
    private User? User;
    protected override async Task OnParametersSetAsync()
    {
        if (UserId is null)
        {
            Back();
            return;
        }

        Guid id = UserId.Value;

        User = await UsersService.GetUserAsync(id.ToString("D"));
        if (User is null)
        {
            Back();
            return;
        }

        NavService.OverridePageTitle(User.Id, User.FullName);
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

        IDialogReference dialog = await DialogService.ShowAsync<StrictConfirmDialog>(null, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
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

    private void Back() => NavigationManager.NavigateTo(AppRoutes.PlatformAdmin.Users);

}
