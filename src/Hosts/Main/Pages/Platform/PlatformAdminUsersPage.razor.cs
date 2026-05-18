using Vitarish.Users.Application.Commands;

namespace Vitarish.Main.Pages.Admins;

public partial class PlatformAdminUsersPage
{
    private List<User> Users { get; set; } = [];
    private User editUser = new();
    private User? editingUser;
    private string password = string.Empty;
    [Inject] private IPasswordHasher<User> PasswordHasher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
        => Users = [.. await UsersService.GetUsersAsync()];

    private void RowClicked(DataGridRowClickEventArgs<User> args)
    {
        editingUser = args.Item;
        editUser = new User
        {
            Id = args.Item.Id,
            UserName = args.Item.UserName,
            FirstName = args.Item.FirstName,
            LastName = args.Item.LastName,
            PhoneNumber = args.Item.PhoneNumber
        };
    }

    private async Task Save()
    {
        if (editingUser is null)
        {
            User? user = await UsersService.CreateUserWithPhoneAsync(new CreateUserWithPhoneCommand(editUser.PhoneNumber, editUser.FirstName, editUser.LastName));
            if (user is not null)
            {
                Users.Add(user);
                _ = Snackbar.Add(Localizer["Created"], Severity.Success);
            }
        }
        else
        {
            editingUser.FirstName = editUser.FirstName;
            editingUser.LastName = editUser.LastName;
            editingUser.PhoneNumber = editUser.PhoneNumber;
            if (!string.IsNullOrWhiteSpace(password))
            {
                editingUser.PasswordHash = PasswordHasher.HashPassword(editingUser, password);
            }
            IdentityResult result = await UsersService.UpdateUserAsync(new UpdateUserCommand(editingUser));
            if (result.Succeeded)
            {
                int idx = Users.FindIndex(u => u.Id == editingUser.Id);
                if (idx >= 0)
                {
                    Users[idx] = editingUser;
                }
                _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
            }
            else
            {
                _ = Snackbar.Add(Localizer["Error"], Severity.Warning);
            }
            editingUser = null;
        }

        CancelEdit();
        StateHasChanged();
    }

    private async Task Remove(User user)
    {
        IdentityResult res = await UsersService.DeleteUserAsync(new DeleteUserCommand(user.Id));
        if (res.Succeeded)
        {
            _ = Users.Remove(user);
            if (editingUser?.Id == user.Id)
            {
                CancelEdit();
            }
            _ = Snackbar.Add(Localizer["Deleted"], Severity.Success);
            StateHasChanged();
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Warning);
        }
    }

    private void CancelEdit()
    {
        editingUser = null;
        editUser = new();
        password = string.Empty;
    }
}
