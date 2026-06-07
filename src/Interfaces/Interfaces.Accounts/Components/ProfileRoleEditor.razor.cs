using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Interfaces.Accounts.Components;

public partial class ProfileRoleEditor
{
    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Parameter] public EventCallback OnUpdate { get; set; }

    private readonly List<RoleDetail> roleDetails = [];
    private bool isLoading;

    protected override async Task ReloadRequired()
    {
        if (User is null)
        {
            return;
        }

        isLoading = true;
        roleDetails.Clear();
        StateHasChanged();

        try
        {
            IEnumerable<TenantReadDto> tenants = await PartiesService.GetTenantsAsync();
            Dictionary<Guid, TenantReadDto> tenantDict = tenants.ToDictionary(t => t.Id);

            List<Guid> ownerIds = [.. tenants.Select(t => t.OwnerId).Distinct()];
            Dictionary<Guid, Task<OwnerReadDto?>> ownerTasks = ownerIds.ToDictionary(id => id, id => PartiesService.GetOwnerAsync(id));
            Dictionary<Guid, OwnerReadDto?> owners = (await Task.WhenAll(
                ownerTasks.Select(async kvp => new { kvp.Key, Value = await kvp.Value })
            )).ToDictionary(x => x.Key, x => x.Value);

            IEnumerable<IdentityRole> userRoles = await UserService.GetUserRolesAsync(User.Id);
            List<string> userRoleNames = [.. userRoles
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => r.Name!)];

            List<RoleDetail> parsedRoles = ParseTenantRoles(userRoleNames, tenantDict, owners);

            roleDetails.AddRange(parsedRoles);
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private async Task RemoveRoleAsync(string roleName)
    {
        if (User is null || string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        bool? confirmed = await DialogService.ShowMessageBoxAsyncAsync(
            null,
            Localizer["DeleteItemConfirm", Localizer["Role"]],
            yesText: Localizer["Remove"],
            cancelText: Localizer["Cancel"]);

        if (confirmed != true)
        {
            return;
        }

        IdentityResult result = await UserService.RemoveUserRoleAsync(User.Id, roleName);
        if (result.Succeeded)
        {
            _ = Snackbar.Add(Localizer["RoleRemoved"], Severity.Success);
            await ReloadRequired();
            await OnUpdate.InvokeAsync();
        }
        else
        {
            _ = Snackbar.Add(Localizer["Error"], Severity.Error);
        }
    }

    private static List<RoleDetail> ParseTenantRoles(
        IEnumerable<string> roles,
        Dictionary<Guid, TenantReadDto> tenantDict,
        Dictionary<Guid, OwnerReadDto?> owners)
    {
        List<RoleDetail> parsedRoles = [];

        foreach (string role in roles)
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

            TenantReadDto? tenant = tenantDict.GetValueOrDefault(tenantId);
            OwnerReadDto? owner = tenant != null ? owners.GetValueOrDefault(tenant.OwnerId) : null;

            string tenantName = tenant?.Name ?? tenantId.ToString();
            string ownerName = owner?.Name ?? tenant?.OwnerId.ToString() ?? "-";
            string ownerId = tenant?.OwnerId.ToString() ?? "-";
            string clientId = tenant?.AppId ?? "-";

            parsedRoles.Add(new RoleDetail(
                tenantId,
                tenantName,
                ownerId,
                ownerName,
                clientId,
                roleName,
                role));
        }

        return parsedRoles;
    }

    private sealed record RoleDetail(
        Guid TenantId,
        string TenantName,
        string OwnerId,
        string OwnerName,
        string ClientId,
        string RoleName,
        string FullRoleName);
}
