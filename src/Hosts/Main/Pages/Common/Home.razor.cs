using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Pages.Clinic.Owners;
using Vitarish.Main.Pages.Commerce.Owners;
using Vitarish.Main.Pages.Sea.Common;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Common;

public partial class Home
{
    [Inject] private IOptions<FeaturesOptions> FeaturesOptions { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    private sealed record DashboardDefinition(
    string Route,
    Type ComponentType
);
    private static readonly Dictionary<string, DashboardDefinition> Dashboards =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sea"] = new(
            AppRoutes.Sea.Dashboard,
            typeof(SeaDashboardPage)
        ),

        ["Clinic"] = new(
            AppRoutes.Clinic.OwnerDashboard,
            typeof(ClinicOwnerDashboardPage)
        ),

        ["Commerce"] = new(
            AppRoutes.Commerce.OwnerDashboard,
            typeof(CommerceOwnerDashboardPage)
        ),
    };

    private IReadOnlyList<(string Name, DashboardDefinition)> activeDashboards = [];
    private bool hasTenantContext;
    private bool onboardingEnabled;

    protected override async Task ReloadRequired()
    {
        onboardingEnabled = FeaturesOptions.Value.TenantOnboarding;
        hasTenantContext = RequestContext.TenantId is not null;

        if (hasTenantContext && Guid.TryParse(RequestContext.TenantId, out Guid tenantGuid))
        {
            TenantReadDto? tenant = await PartiesService.GetTenantAsync(tenantGuid);
            if (tenant is not null)
            {
                IReadOnlyList<string> modules = TenantMetadata.GetActiveModules(tenant.Metadata);
                IReadOnlyList<string> apps = TenantMetadata.GetActiveApps(tenant.Metadata);

                activeDashboards = [.. modules.Union(apps)
                    .Where(Dashboards.ContainsKey)
                    .Select(m => (m, Dashboards[m]))];
            }
        }
    }

    private Task<Action> NavigateToTenant(string route) => Task.FromResult(() => NavigationManager.NavigateTo(route));
}
