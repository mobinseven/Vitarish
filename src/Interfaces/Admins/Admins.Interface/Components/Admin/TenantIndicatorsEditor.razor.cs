using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantIndicatorsEditor
{
    [Parameter, EditorRequired] public TenantReadDto Tenant { get; set; }

    private List<TenantIndicator> Indicators { get; set; } = [];

    protected override Task ReloadRequired()
    {
        // TODO: TenantReadDto does not include Indicators collection.
        // To make this component functional, either:
        // 1. Add indicators to TenantReadDto
        // 2. Create a separate IIndicatorsService
        // 3. Add a method to IPartiesService to get tenant with indicators
        Indicators.Clear();
        return Task.CompletedTask;
    }

    protected async Task AddIndicator()
    {
        DialogParameters parameters = new()
        {
            {"TenantId", Tenant.Id }
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
            {"TenantId", Tenant.Id },
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
