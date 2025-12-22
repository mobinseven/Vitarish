using Microsoft.AspNetCore.Identity;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class UserRoleAssignments : AdminBase
{
    private readonly List<UserRoleEntry> users = [];
    private readonly List<string> roles = [];
    private bool isLoading;

    [Inject] private UserManager<User> UserManager { get; set; } = default!;
    [Inject] private RoleManager<IdentityRole> RoleManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        users.Clear();
        roles.Clear();
        StateHasChanged();

        IEnumerable<User> loadedUsers = await UserService.GetUsersAsync();
        roles.AddRange(RoleManager.Roles
            .Select(role => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))!
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name));

        foreach (User user in loadedUsers)
        {
            IList<string> userRoles = await UserManager.GetRolesAsync(user);
            users.Add(new UserRoleEntry(user, userRoles.ToList()));
        }

        isLoading = false;
        StateHasChanged();
    }

    private Task ReloadAsync() => LoadAsync();

    private Task UpdateSelectedRole(UserRoleEntry entry, string? value)
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

        IdentityResult result = await UserManager.AddToRoleAsync(entry.User, entry.SelectedRole);
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

        IdentityResult result = await UserManager.RemoveFromRoleAsync(entry.User, role);
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
}
