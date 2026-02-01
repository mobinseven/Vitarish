using Vitarish.Parties.Application.Dtos;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantIndicatorsEditor
{
    [Parameter, EditorRequired] public TenantReadDto Tenant { get; set; }
    [Inject] private TenantService TenantService { get; set; } = default!;

    private List<TenantIndicator> Indicators { get; set; } = [];

    protected override async Task ReloadRequired()
    {
        // Reload the tenant entity to get updated indicators
        var result = await TenantService.GetEntityAsync(Tenant.Id);
        if (result.IsSuccess && result.Value is not null)
        {
            Indicators = [.. result.Value.Indicators];
        }
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
