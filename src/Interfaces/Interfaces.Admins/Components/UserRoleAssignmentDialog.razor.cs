using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;

namespace Vitarish.Interfaces.Admins.Components;

public partial class UserRoleAssignmentDialog
{
    private readonly List<TenantRoleOption> tenantRoles = [];
    private bool isLoading;
    private TenantRoleOption? selectedRole;

    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter] public string UserName { get; set; } = string.Empty;
    [Parameter] public List<string> AvailableRoles { get; set; } = [];
    [Parameter] public List<string> AssignedRoles { get; set; } = [];

    [Inject] private IPartiesService PartiesService { get; set; } = default!;
    [Inject] private IStringLocalizer Localizer { get; set; } = default!;

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
            IEnumerable<TenantReadDto> tenants = await PartiesService.GetTenantsAsync();
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

    private void Assign()
    {
        if (selectedRole == null)
        {
            return;
        }

        MudDialog.Close(DialogResult.Ok(selectedRole.FullRoleName));
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
}
