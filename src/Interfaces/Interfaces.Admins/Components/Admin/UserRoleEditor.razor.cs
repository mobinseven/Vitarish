using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Interfaces.Admins.Components.Admin;

public partial class UserRoleEditor
{
    [Parameter, EditorRequired] public User User { get; set; }
    [Parameter] public EventCallback OnUpdate { get; set; }

    UserRoleEntry? userRole;
    private readonly List<string> roles = [];
    private bool isLoading;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync(User);
    }

    private async Task LoadAsync(User user)
    {
        isLoading = true;
        roles.Clear();
        StateHasChanged();

        try
        {
            Task<IEnumerable<TenantReadDto>> tenantsTask = PartiesService.GetTenantsAsync();

            IEnumerable<TenantReadDto> tenants = await tenantsTask;
            Dictionary<Guid, TenantReadDto> tenantDict = tenants.ToDictionary(t => t.Id);

            List<Guid> ownerIds = tenants.Select(t => t.OwnerId).Distinct().ToList();
            Dictionary<Guid, Task<OwnerReadDto?>> ownerTasks = ownerIds.ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));
            Dictionary<Guid, OwnerReadDto?> owners = (await Task.WhenAll(
                ownerTasks.Select(async kvp => new { kvp.Key, Value = await kvp.Value })
            )).ToDictionary(x => x.Key, x => x.Value);

            List<string> clientIds = tenants
                .Where(t => !string.IsNullOrWhiteSpace(t.ClientId))
                .Select(t => t.ClientId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            roles.AddRange(RoleManager.Roles
                .Where(role => role.Name != null)
                .Select(role => role.Name!));

            IList<string> userRoles = await UserManager.GetRolesAsync(user);
            List<ParsedTenantRole> parsedRoles = RoleParser.ParseTenantRoles(userRoles, tenantDict, owners);

            List<RoleDetail> roleDetails = parsedRoles
                .Select(r => new RoleDetail(r.TenantId, r.TenantName, r.OwnerId, r.OwnerName, r.ClientId, r.RoleName, r.FullRoleName))
                .ToList();

            userRole = new UserRoleEntry(user, [.. userRoles], roleDetails);

        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
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
            await OnUpdate.InvokeAsync();
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
            await OnUpdate.InvokeAsync();
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
