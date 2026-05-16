using Microsoft.Extensions.Options;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Parties.Domain;
using Vitarish.Parties.Domain.Dtos;

namespace Vitarish.Main.Pages.Common;

public partial class Home
{
    [Inject] private IOptions<FeaturesOptions> FeaturesOptions { get; set; } = default!;
    [Inject] private IPartiesService PartiesService { get; set; } = default!;

    /// <summary>
    /// Module names that expose a meaningful dashboard route within a tenant context.
    /// Extend this map as further modules gain tenant-scoped dashboard pages.
    /// </summary>
    private static readonly Dictionary<string, string> ModuleDashboardRoutes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Sea"]          = AppRoutes.Sea.Dashboard,
            ["Medical"]      = AppRoutes.Medical.Dashboard,
            ["Commerce"]     = AppRoutes.Commerce.OwnerDashboard,
            ["Appointments"] = AppRoutes.Appointments.AdminShifts,
        };

    private IReadOnlyList<(string Name, string Route)> activeDashboards = [];
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

                activeDashboards = [.. modules
                    .Where(m => ModuleDashboardRoutes.ContainsKey(m))
                    .Select(m => (m, ModuleDashboardRoutes[m]))];
            }
        }
    }

    private Task NavigateToTenant(string route)
    {
        NavigationManager.NavigateTo(route, forceLoad: true);
        return Task.CompletedTask;
    }
}
