using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Domain.Entities;
using ClaimDto = Vitarish.Users.Domain.Models.ClaimDto;

namespace Vitarish.Accounts.Interface.Components.Admin;

public partial class RolesManager : AdminBase
{
    private readonly List<RoleEntry> roles = [];
    private readonly List<Tenant> tenants = [];
    private bool isLoading;
    private string newRoleName = string.Empty;
    private string? selectedTenantId;
    private List<RoleEntry> tenantRoles => selectedTenantId is null ? [] : [.. roles.Where(r => r.Role.Name?.StartsWith(selectedTenantId) ?? false)];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadRolesAsync();
    }

    private async Task LoadRolesAsync()
    {
        isLoading = true;
        roles.Clear();
        tenants.Clear();
        StateHasChanged();

        try
        {
            Task<List<IdentityRole>> rolesTask = RoleManager.Roles
                .OrderBy(role => role.Name)
                .ToListAsync();

            Task<IEnumerable<Tenant>> tenantsTask = PartiesService.GetTenantsAsync();

            await Task.WhenAll(rolesTask, tenantsTask);

            List<IdentityRole> loadedRoles = await rolesTask;
            tenants.AddRange((await tenantsTask).OrderBy(tenant => tenant.Name));
            selectedTenantId ??= tenants.FirstOrDefault()?.Id.ToString();

            IList<Claim>[] claimSets = new IList<Claim>[loadedRoles.Count];

            int index = 0;
            foreach (IdentityRole role in loadedRoles)
            {
                claimSets[index] = await RoleManager.GetClaimsAsync(role);
                index++;
            }

            foreach (RoleEntry? role in loadedRoles.Zip(claimSets, (role, claims) => new RoleEntry(role, [.. claims.Select(claim => new ClaimDto(claim.Type, claim.Value))])))
            {
                roles.Add(role);
            }


        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task CreateRoleAsync()
    {
        string roleName = newRoleName.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            _ = Snackbar.Add(Localizer["RoleNameRequired"], Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(selectedTenantId))
        {
            _ = Snackbar.Add(Localizer["SelectTenant"], Severity.Warning);
            return;
        }

        string tenantRoleName = $"{selectedTenantId}:{roleName}";

        if (await RoleManager.RoleExistsAsync(tenantRoleName))
        {
            _ = Snackbar.Add(Localizer["RoleExists", tenantRoleName], Severity.Info);
            return;
        }

        IdentityResult result = await RoleManager.CreateAsync(new IdentityRole(tenantRoleName));
        if (!result.Succeeded)
        {
            _ = Snackbar.Add(Localizer["RoleCreateFailed"], Severity.Error);
            return;
        }

        newRoleName = string.Empty;
        selectedTenantId = tenants.FirstOrDefault()?.Id.ToString();
        IdentityRole? createdRole = await RoleManager.FindByNameAsync(tenantRoleName);
        if (createdRole is not null)
        {
            roles.Add(new RoleEntry(createdRole, []));
            roles.Sort(RoleComparison);
        }

        _ = Snackbar.Add(Localizer["RoleCreated"], Severity.Success);
    }

    private void StartEdit(RoleEntry entry)
    {
        entry.EditName = entry.Role.Name ?? string.Empty;
        entry.IsEditing = true;
    }

    private void CancelEdit(RoleEntry entry)
    {
        entry.EditName = entry.Role.Name ?? string.Empty;
        entry.IsEditing = false;
    }

    private async Task SaveRoleAsync(RoleEntry entry)
    {
        string roleName = entry.EditName.Trim();
        if (string.IsNullOrWhiteSpace(roleName))
        {
            _ = Snackbar.Add(Localizer["RoleNameRequired"], Severity.Warning);
            return;
        }

        if (!string.Equals(roleName, entry.Role.Name, StringComparison.OrdinalIgnoreCase)
            && await RoleManager.RoleExistsAsync(roleName))
        {
            _ = Snackbar.Add(Localizer["RoleExists", roleName], Severity.Warning);
            return;
        }

        entry.IsProcessing = true;

        entry.Role.Name = roleName;
        IdentityResult result = await RoleManager.UpdateAsync(entry.Role);

        entry.IsProcessing = false;

        if (result.Succeeded)
        {
            entry.IsEditing = false;
            entry.EditName = entry.Role.Name ?? string.Empty;
            roles.Sort(RoleComparison);
            _ = Snackbar.Add(Localizer["RoleUpdated"], Severity.Success);
        }
        else
        {
            _ = Snackbar.Add(Localizer["RoleUpdateFailed"], Severity.Error);
        }
    }

    private async Task ManageClaimsAsync(RoleEntry entry)
    {
        DialogParameters<RoleClaimsDialog> parameters = new()
        {
            { dialog => dialog.RoleName, entry.Role.Name ?? string.Empty },
            { dialog => dialog.Claims, entry.Claims }
        };

        DialogOptions options = new()
        {
            CloseButton = true,
            FullWidth = true,
            MaxWidth = MaxWidth.Medium
        };

        IDialogReference dialog = await DialogService.ShowAsync<RoleClaimsDialog>(Localizer["ManageClaims"], parameters, options);
        DialogResult result = await dialog.Result;

        if (result is { Canceled: false, Data: List<ClaimDto> updatedClaims })
        {
            await SaveClaimsAsync(entry, updatedClaims);
        }
    }

    private async Task SaveClaimsAsync(RoleEntry entry, List<ClaimDto> updatedClaims)
    {
        entry.IsProcessing = true;

        List<Claim> existing = entry.Claims.Select(claim => claim.ToClaim()).ToList();
        List<Claim> desired = updatedClaims.Select(claim => claim.ToClaim()).ToList();

        ILookup<string, Claim> desiredLookup = desired.ToLookup(c => c.Type, StringComparer.OrdinalIgnoreCase);
        List<Claim> toRemove = existing
            .Where(e => !desiredLookup[e.Type].Any(d => string.Equals(d.Value, e.Value, StringComparison.Ordinal)))
            .ToList();

        ILookup<string, Claim> existingLookup = existing.ToLookup(c => c.Type, StringComparer.OrdinalIgnoreCase);
        List<Claim> toAdd = desired
            .Where(d => !existingLookup[d.Type].Any(e => string.Equals(e.Value, d.Value, StringComparison.Ordinal)))
            .ToList();

        foreach (Claim claim in toRemove)
        {
            IdentityResult result = await RoleManager.RemoveClaimAsync(entry.Role, claim);
            if (!result.Succeeded)
            {
                entry.IsProcessing = false;
                _ = Snackbar.Add(Localizer["ClaimsUpdateFailed"], Severity.Error);
                return;
            }
        }

        foreach (Claim claim in toAdd)
        {
            IdentityResult result = await RoleManager.AddClaimAsync(entry.Role, claim);
            if (!result.Succeeded)
            {
                entry.IsProcessing = false;
                _ = Snackbar.Add(Localizer["ClaimsUpdateFailed"], Severity.Error);
                return;
            }
        }

        entry.Claims = updatedClaims;
        entry.IsProcessing = false;
        _ = Snackbar.Add(Localizer["ClaimsUpdated"], Severity.Success);
    }

    private async Task DeleteRoleAsync(RoleEntry entry)
    {
        bool? confirmed = await DialogService.ShowMessageBox(
            Localizer["Delete"],
            Localizer["RoleDeleteConfirmation", entry.Role.Name ?? string.Empty],
            yesText: Localizer["Delete"],
            cancelText: Localizer["Cancel"],
            options: new DialogOptions { CloseButton = true });

        if (confirmed != true)
        {
            return;
        }

        entry.IsProcessing = true;
        IdentityResult result = await RoleManager.DeleteAsync(entry.Role);
        entry.IsProcessing = false;

        if (result.Succeeded)
        {
            _ = roles.Remove(entry);
            _ = Snackbar.Add(Localizer["RoleDeleted"], Severity.Success);
        }
        else
        {
            _ = Snackbar.Add(Localizer["RoleDeleteFailed"], Severity.Error);
        }
    }

    private static bool ClaimEquals(Claim left, Claim right) =>
        string.Equals(left.Type, right.Type, StringComparison.OrdinalIgnoreCase)
        && string.Equals(left.Value, right.Value, StringComparison.Ordinal);

    private static int RoleComparison(RoleEntry left, RoleEntry right)
        => string.Compare(left.Role.Name ?? string.Empty, right.Role.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private sealed class RoleEntry(IdentityRole role, List<ClaimDto> claims)
    {
        public IdentityRole Role { get; } = role;
        public string? Name => Role.Name?.Substring(Role.Name.IndexOf(':') + 1, Role.Name.Length - Role.Name.IndexOf(':') - 1);

        public List<ClaimDto> Claims { get; set; } = claims;

        public bool IsEditing { get; set; }

        public bool IsProcessing { get; set; }

        public string EditName { get; set; } = role.Name ?? string.Empty;
    }
}
