using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Modules.Aggregator.Interface.Components.Admins;

public partial class UserRoleAssignmentDialog
{
    private readonly List<TenantRoleOption> tenantRoles = [];
    private bool isLoading;
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public List<string> AvailableRoles { get; set; } = [];
    [Parameter] public List<string> AssignedRoles { get; set; } = [];

    
    [Inject] private new IStringLocalizer Localizer { get; set; } = default!;

    private List<string> assignedRoles = [];

    protected override async Task OnInitializedAsync()
    {
        assignedRoles = [.. AssignedRoles];
        await LoadTenantRolesAsync();
    }

    private async Task LoadTenantRolesAsync()
    {
        isLoading = true;
        tenantRoles.Clear();
        StateHasChanged();

        try
        {
            var tenantsResult = await PartiesClient.GetTenantsAsync();
            if (!tenantsResult.IsSuccess || tenantsResult.Value is null)
            {
                Snackbar.Add(Localizer["Error"], Severity.Error);
                return;
            }

            IEnumerable<TenantReadDto> tenants = tenantsResult.Value;
            Dictionary<Guid, TenantReadDto> tenantDict = tenants.ToDictionary(t => t.Id);

            List<Guid> ownerIds = [.. tenants.Select(t => t.OwnerId).Distinct()];
            Dictionary<Guid, Task<OwnerReadDto?>> ownerTasks = ownerIds.ToDictionary(id => id, id => GetOwnerOrNullAsync(id));
            Dictionary<Guid, OwnerReadDto?> owners = (await Task.WhenAll(
                ownerTasks.Select(async kvp => new { kvp.Key, Value = await kvp.Value })
            )).ToDictionary(x => x.Key, x => x.Value);

            List<string> clientIds = [.. tenants
                .Where(t => !string.IsNullOrWhiteSpace(t.AppId))
                .Select(t => t.AppId!)
                .Distinct(StringComparer.OrdinalIgnoreCase)];

            List<ParsedTenantRole> parsedRoles = RoleParser.ParseTenantRoles(AvailableRoles, tenantDict, owners);

            foreach (ParsedTenantRole parsed in parsedRoles)
            {
                tenantRoles.Add(new TenantRoleOption(
                    parsed.TenantId,
                    parsed.TenantName,
                    parsed.OwnerName,
                    parsed.ClientId,
                    parsed.RoleName,
                    parsed.FullRoleName));
            }

            tenantRoles.Sort((a, b) =>
            {
                int tenantCompare = string.Compare(a.TenantName, b.TenantName, StringComparison.OrdinalIgnoreCase);
                return tenantCompare != 0 ? tenantCompare : string.Compare(a.RoleName, b.RoleName, StringComparison.OrdinalIgnoreCase);
            });
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private void Assign(TenantRoleOption tenantRole)
    {
        if (tenantRole == null)
        {
            return;
        }

        MudDialog.Close(DialogResult.Ok(tenantRole.FullRoleName));
    }

    private sealed record TenantRoleOption(
        Guid TenantId,
        string TenantName,
        string OwnerName,
        string ClientId,
        string RoleName,
        string FullRoleName)
    {
        public string DisplayName => $"{RoleName} ({TenantName})";
    }
    private async Task<OwnerReadDto?> GetOwnerOrNullAsync(Guid ownerId)
    {
        var ownerResult = await PartiesClient.GetOwnerAsync(ownerId);
        return ownerResult.IsSuccess ? ownerResult.Value : null;
    }

}
