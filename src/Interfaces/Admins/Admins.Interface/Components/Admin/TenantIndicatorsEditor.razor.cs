using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantIndicatorsEditor
{
    [Parameter, EditorRequired] public Tenant Tenant { get; set; }

    private List<TenantIndicator> Indicators { get; set; } = [];

    protected override async Task ReloadRequired()
    {
        Indicators = [.. await PartiesService.GetTenantIndicatorsAsync(Tenant.Id)];
    }

    protected async Task AddIndicator()
    {
        DialogParameters parameters = new()
        {
            {"Tenant", Tenant }
        };
        var dialog = await DialogService.ShowAsync<TenantIndicatorEditDialog>(null, parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        await ReloadRequired();
    }


    protected async Task EditIndicator(TenantIndicator tenantIndicator)
    {
        DialogParameters parameters = new()
        {
            {"Tenant", Tenant },
            {"TenantIndicator", tenantIndicator }
        };
        var dialog = await DialogService.ShowAsync<TenantIndicatorEditDialog>(null, parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        await ReloadRequired();
    }

    private string GetStatusIcon(IndicatorStatus status) => status switch
    {
        IndicatorStatus.PendingVerification => "fa-regular fa-seal-exclamation",
        IndicatorStatus.Verified => "fa-regular fa-check",
        IndicatorStatus.Active => "fa-regular fa-check-circle",
        _ => "fa-regular fa-pause"
    };
}
