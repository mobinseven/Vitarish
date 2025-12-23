using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class UserRoleAssignments : AdminBase
{
    private readonly List<UserRoleEntry> users = [];
    private readonly List<string> roles = [];
    private readonly List<Tenant> tenants = [];
    private bool isLoading;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        isLoading = true;
        users.Clear();
        roles.Clear();
        tenants.Clear();
        StateHasChanged();

        try
        {
            Task<IEnumerable<User>> usersTask = UserService.GetUsersAsync();
            Task<IEnumerable<Tenant>> tenantsTask = PartiesService.GetTenantsAsync();

            await Task.WhenAll(usersTask, tenantsTask);

            List<User> loadedUsers = (await usersTask).ToList();
            tenants.AddRange((await tenantsTask).OrderBy(tenant => tenant.Name));
            roles.AddRange(RoleManager.Roles
                .Where(role=>role.Name != null)
                .Select(role => role.Name)
                .Distinct()
                .Cast<string>());

            IList<string>[] userRoles = await Task.WhenAll(loadedUsers.Select(UserManager.GetRolesAsync));

            users.AddRange(loadedUsers.Zip(
                userRoles,
                (user, roles) => new UserRoleEntry(user, [.. roles])));
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private Task ReloadAsync() => LoadAsync();

    private Task UpdateSelectedRole(UserRoleEntry entry, string? value)
    {
        entry.SelectedRole = value;
        return Task.CompletedTask;
    }

    private Task UpdateSelectedTenant(UserRoleEntry entry, string? value)
    {
        entry.SelectedTenantId = value;
        return Task.CompletedTask;
    }

    private async Task AssignRoleAsync(UserRoleEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.SelectedRole))
        {
            _ = Snackbar.Add(Localizer["SelectRole"], Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.SelectedTenantId))
        {
            _ = Snackbar.Add(Localizer["SelectTenant"], Severity.Warning);
            return;
        }

        string tenantRole = $"{entry.SelectedTenantId}:{entry.SelectedRole}";

        if (entry.AssignedRoles.Contains(tenantRole, StringComparer.OrdinalIgnoreCase))
        {
            _ = Snackbar.Add(Localizer["RoleAlreadyAssigned"], Severity.Info);
            entry.SelectedRole = null;
            entry.SelectedTenantId = null;
            return;
        }

        IdentityResult result = await UserManager.AddToRoleAsync(entry.User, tenantRole);
        if (result.Succeeded)
        {
            entry.AssignedRoles.Add(tenantRole);
            _ = Snackbar.Add(Localizer["RoleAssigned"], Severity.Success);
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }

        entry.SelectedRole = null;
        entry.SelectedTenantId = null;
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
        public string? SelectedTenantId { get; set; }
    }
}
