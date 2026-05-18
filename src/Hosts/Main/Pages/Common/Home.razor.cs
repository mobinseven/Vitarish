using Microsoft.Extensions.Options;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Pages.Clinic;
using Vitarish.Main.Pages.Commerce.Owners;
using Vitarish.Main.Pages.Sea.Common;
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
    private sealed record ModuleDashboardDefinition(
    string Route,
    Type ComponentType
);
    private static readonly Dictionary<string, ModuleDashboardDefinition> ModuleDashboards =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sea"] = new(
            AppRoutes.Sea.Dashboard,
            typeof(SeaDashboardPage)
        ),

        ["Clinic"] = new(
            AppRoutes.Clinic.OwnerDashboard,
            typeof(ClinicDashboardPage)
        ),

        ["Commerce"] = new(
            AppRoutes.Commerce.OwnerDashboard,
            typeof(CommerceOwnerDashboardPage)
        ),
    };

    private IReadOnlyList<(string Name, ModuleDashboardDefinition)> activeDashboards = [];
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
                    .Where(m => ModuleDashboards.ContainsKey(m))
                    .Select(m => (m, ModuleDashboards[m]))];
            }
        }
    }

    private Task NavigateToTenant(string route)
    {
        return Task.CompletedTask;
    }
}
