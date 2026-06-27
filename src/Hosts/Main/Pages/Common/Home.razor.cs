using Vitarish.Main.Pages.Clinic.Owners;
using Vitarish.Main.Pages.Commerce.Owners;
using Vitarish.Main.Pages.Sea.Common;
using Vitarish.Parties.Client;
using Vitarish.Parties.Contracts.Results;
using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Pages.Common;

public partial class Home
{
    [Inject] private IPartiesClient PartiesClient { get; set; } = default!;

    private bool hasTenantContext;

    private static readonly Dictionary<string, string> Dashboards =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["Clinic"] = AppRoutes.Clinic.OwnerDashboard,
    };

    protected override async Task OnInitializedAsync()
    {
        hasTenantContext = RequestContext.TenantId is not null;

        if (hasTenantContext && Guid.TryParse(RequestContext.TenantId, out Guid tenantGuid))
        {
            PartiesResult<TenantReadDto> tenantResult = await PartiesClient.GetTenantAsync(tenantGuid);
            if (tenantResult.IsSuccess && tenantResult.Value is { } tenant)
            {
                IReadOnlyList<string> apps = TenantMetadataValues.GetActiveApps(tenant.Metadata);
                var app = apps.FirstOrDefault();
                if(app is null)
                {
                    return;
                }
                Dashboards.TryGetValue(app, out string? route);

                if (route is null)
                {
                    return;
                }

                NavigationManager.NavigateTo(route);
            }
        }
    }

    private Task<Action> NavigateToTenant(string route) => Task.FromResult(() => NavigationManager.NavigateTo(route));
}
