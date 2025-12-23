using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class UserRoleAssignments : AdminBase
{
    private readonly List<UserRoleEntry> users = [];
    private readonly List<string> roles = [];
    private bool isLoading;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        users.Clear();
        roles.Clear();
        StateHasChanged();

        try
        {
            Task<IEnumerable<User>> usersTask = UserService.GetUsersAsync();

            List<User> loadedUsers = [.. await usersTask];
            roles.AddRange(RoleManager.Roles
                .Where(role=>role.Name != null)
                .Select(role => role.Name)
                .Distinct()
                .Cast<string>());

            foreach (User user in loadedUsers)
            {
                IList<string> userRoles = await UserManager.GetRolesAsync(user);
                users.Add(new UserRoleEntry(user, [.. userRoles]));
            }
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private Task ReloadAsync() => LoadAsync();

    private static Task UpdateSelectedRole(UserRoleEntry entry, string? value)
    {
        entry.SelectedRole = value;
        return Task.CompletedTask;
    }

    private async Task AssignRoleAsync(UserRoleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SelectedRole))
        {
            _ = Snackbar.Add(Localizer["SelectRole"], Severity.Warning);
            return;
        }

        if (entry.AssignedRoles.Contains(entry.SelectedRole, StringComparer.OrdinalIgnoreCase))
        {
            _ = Snackbar.Add(Localizer["RoleAlreadyAssigned"], Severity.Info);
            entry.SelectedRole = null;
            return;
        }

        User? user = await GetUserAsync(entry);
        if (user is null)
        {
            return;
        }

        IdentityResult result = await UserManager.AddToRoleAsync(user, entry.SelectedRole);
        if (result.Succeeded)
        {
            entry.AssignedRoles.Add(entry.SelectedRole);
            _ = Snackbar.Add(Localizer["RoleAssigned"], Severity.Success);
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }

        entry.SelectedRole = null;
        StateHasChanged();
    }

    private async Task RemoveRoleAsync(UserRoleEntry entry, string role)
    {
        if (!entry.AssignedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        User? user = await GetUserAsync(entry);
        if (user is null)
        {
            return;
        }

        IdentityResult result = await UserManager.RemoveFromRoleAsync(user, role);
        if (result.Succeeded)
        {
            _ = entry.AssignedRoles.RemoveAll(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
            _ = Snackbar.Add(Localizer["RoleRemoved"], Severity.Success);
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }

        StateHasChanged();
    }

    private sealed record UserRoleEntry(User User, List<string> AssignedRoles)
    {
        public string? SelectedRole { get; set; }
    }

    private async Task<User?> GetUserAsync(UserRoleEntry entry)
    {
        string? userId = entry?.User?.Id;
        if (string.IsNullOrWhiteSpace(userId))
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
            return null;
        }

        User? user = await UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
            return null;
        }

        return user;
    }
}
