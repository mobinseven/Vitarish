using Vorn.Aaas.Users.Application.Commands;

namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

public partial class AdminUsersPage
{
    List<User> Users { get; set; } = [];
    User editUser = new();
    User? editingUser;
    string password = string.Empty;
    [Inject] IPasswordHasher<User> PasswordHasher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
        => Users = [.. await UserService.GetUsersAsync()];

    void RowClicked(DataGridRowClickEventArgs<User> args)
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

    async Task Save()
    {
        if (editingUser is null)
        {
            User? user = await UserService.CreateUserWithPhoneAsync(new CreateUserWithPhoneCommand(editUser.PhoneNumber, editUser.FirstName, editUser.LastName));
            if (user is not null)
            {
                Users.Add(user);
                Snackbar.Add(Localizer["Created"], Severity.Success);
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
            var result = await UserService.UpdateUserAsync(new UpdateUserCommand(editingUser));
            if (result.Succeeded)
            {
                int idx = Users.FindIndex(u => u.Id == editingUser.Id);
                if (idx >= 0)
                {
                    Users[idx] = editingUser;
                }
                Snackbar.Add(Localizer["Saved"], Severity.Success);
            }
            else
            {
                Snackbar.Add(Localizer["Error"], Severity.Warning);
            }
            editingUser = null;
        }

        CancelEdit();
        StateHasChanged();
    }

    async Task Remove(User user)
    {
        IdentityResult res = await UserService.DeleteUserAsync(new DeleteUserCommand(user.Id));
        if (res.Succeeded)
        {
            Users.Remove(user);
            if (editingUser?.Id == user.Id)
            {
                CancelEdit();
            }
            Snackbar.Add(Localizer["Deleted"], Severity.Success);
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(Localizer["Error"], Severity.Warning);
        }
    }

    void CancelEdit()
    {
        editingUser = null;
        editUser = new();
        password = string.Empty;
    }
}
