using Vitarish.Parties.Domain.Entities;
using Vitarish.ApiClients.Domain.Entities;
using MudBlazor;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class UserRoleAssignments : AdminBase
{
    private readonly List<UserRoleEntry> users = [];
    private readonly List<string> roles = [];
    private bool isLoading;

    [Inject] private IDialogService DialogService { get; set; } = default!;

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
            Task<IEnumerable<Tenant>> tenantsTask = PartiesService.GetTenantsAsync();

            List<User> loadedUsers = [.. await usersTask];
            IEnumerable<Tenant> tenants = await tenantsTask;
            Dictionary<Guid, Tenant> tenantDict = tenants.ToDictionary(t => t.Id);

            List<Guid> ownerIds = tenants.Select(t => t.OwnerId).Distinct().ToList();
            Dictionary<Guid, Task<Owner?>> ownerTasks = ownerIds.ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));
            await Task.WhenAll(ownerTasks.Values);
            Dictionary<Guid, Owner?> owners = ownerTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetAwaiter().GetResult());

            List<string> clientIds = tenants
                .Where(t => !string.IsNullOrWhiteSpace(t.ClientId))
                .Select(t => t.ClientId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Dictionary<string, Task<ApiClient?>> clientTasks = clientIds
                .ToDictionary(id => id, id => ApiClientService.GetApiClientAsync(id), StringComparer.OrdinalIgnoreCase);
            await Task.WhenAll(clientTasks.Values);
            Dictionary<string, ApiClient?> clients = clientTasks
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GetAwaiter().GetResult(), StringComparer.OrdinalIgnoreCase);

            roles.AddRange(RoleManager.Roles
                .Where(role => role.Name != null)
                .Select(role => role.Name!));

            foreach (User user in loadedUsers)
            {
                IList<string> userRoles = await UserManager.GetRolesAsync(user);
                List<RoleDetail> roleDetails = [];

                foreach (string role in userRoles)
                {
                    int separatorIndex = role.IndexOf(':');
                    if (separatorIndex <= 0)
                    {
                        continue;
                    }

                    string tenantIdText = role[..separatorIndex];
                    string roleName = role[(separatorIndex + 1)..];

                    if (string.IsNullOrWhiteSpace(roleName) || !Guid.TryParse(tenantIdText, out Guid tenantId))
                    {
                        continue;
                    }

                    Tenant? tenant = tenantDict.GetValueOrDefault(tenantId);
                    Owner? owner = tenant != null ? owners.GetValueOrDefault(tenant.OwnerId) : null;
                    ApiClient? client = tenant != null && !string.IsNullOrWhiteSpace(tenant.ClientId)
                        ? clients.GetValueOrDefault(tenant.ClientId)
                        : null;

                    string tenantName = tenant?.Name ?? tenantId.ToString();
                    string ownerName = owner?.Name ?? (tenant?.OwnerId.ToString() ?? "-");
                    string ownerId = tenant?.OwnerId.ToString() ?? "-";
                    string clientId = client?.Id ?? (tenant?.ClientId ?? "-");

                    roleDetails.Add(new RoleDetail(
                        tenantId,
                        tenantName,
                        ownerId,
                        ownerName,
                        clientId,
                        roleName,
                        role));
                }

                users.Add(new UserRoleEntry(user, [.. userRoles], roleDetails));
            }
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private Task ReloadAsync() => LoadAsync();

    private void ToggleExpand(UserRoleEntry entry)
    {
        entry.IsExpanded = !entry.IsExpanded;
        StateHasChanged();
    }

    private async Task OpenAssignRoleDialogAsync(UserRoleEntry entry)
    {
        DialogParameters<UserRoleAssignmentDialog> parameters = new()
        {
            { dialog => dialog.UserName, entry.User.UserName ?? entry.User.Email ?? "User" },
            { dialog => dialog.AvailableRoles, roles },
            { dialog => dialog.AssignedRoles, entry.AssignedRoles }
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        IDialogReference dialog = await DialogService.ShowAsync<UserRoleAssignmentDialog>(
            Localizer["AssignRole"],
            parameters,
            options);
        DialogResult result = await dialog.Result;

        if (result is { Canceled: false, Data: string selectedRole })
        {
            await AssignRoleAsync(entry, selectedRole);
        }
    }

    private async Task AssignRoleAsync(UserRoleEntry entry, string selectedRole)
    {
        if (string.IsNullOrWhiteSpace(selectedRole))
        {
            _ = Snackbar.Add(Localizer["SelectRole"], Severity.Warning);
            return;
        }

        if (entry.AssignedRoles.Contains(selectedRole, StringComparer.OrdinalIgnoreCase))
        {
            _ = Snackbar.Add(Localizer["RoleAlreadyAssigned"], Severity.Info);
            return;
        }

        User? user = await GetUserAsync(entry);
        if (user is null)
        {
            return;
        }

        IdentityResult result = await UserManager.AddToRoleAsync(user, selectedRole);
        if (result.Succeeded)
        {
            _ = Snackbar.Add(Localizer["RoleAssigned"], Severity.Success);
            await ReloadAsync();
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
    }

    private async Task RemoveRoleAsync(UserRoleEntry entry, string role)
    {
        if (!entry.AssignedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        bool? confirmed = await DialogService.ShowMessageBox(
            Localizer["Confirm"],
            Localizer["ConfirmRemoveRole", role],
            yesText: Localizer["Remove"],
            cancelText: Localizer["Cancel"]);

        if (confirmed != true)
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
            _ = Snackbar.Add(Localizer["RoleRemoved"], Severity.Success);
            await ReloadAsync();
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
    }

    private sealed class UserRoleEntry
    {
        public UserRoleEntry(User user, List<string> assignedRoles, List<RoleDetail> roleDetails)
        {
            User = user;
            AssignedRoles = assignedRoles;
            RoleDetails = roleDetails;
        }

        public User User { get; }
        public List<string> AssignedRoles { get; }
        public List<RoleDetail> RoleDetails { get; }
        public bool IsExpanded { get; set; }
    }

    private sealed record RoleDetail(
        Guid TenantId,
        string TenantName,
        string OwnerId,
        string OwnerName,
        string ClientId,
        string RoleName,
        string FullRoleName);

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
