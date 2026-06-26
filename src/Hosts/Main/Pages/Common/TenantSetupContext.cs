using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Main.Pages.Common;

public enum SetupStep { Details, Indicator, Modules }

/// <summary>
/// Holds all state collected by the tenant-onboarding wizard.
/// </summary>
public class TenantSetupContext
{
    public SetupStep CurrentStep { get; set; } = SetupStep.Details;

    // ── Step 1 – Workspace details ────────────────────────────────────────────
    public string OwnerName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;

    // ── Step 2 – Tenant indicator ─────────────────────────────────────────────
    public IndicatorMethod IndicatorMethod { get; set; } = IndicatorMethod.Subdomain;
    public string IndicatorValue { get; set; } = string.Empty;

    // ── Step 3 – Module selection ─────────────────────────────────────────────
    public HashSet<string> SelectedModules { get; } = [];
}
