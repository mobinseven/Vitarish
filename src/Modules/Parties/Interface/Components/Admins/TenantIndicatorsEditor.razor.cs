using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Interface.Components.Admins;

public partial class TenantIndicatorsEditor
{
    [Parameter, EditorRequired] public TenantReadDto Tenant { get; set; }

    private List<TenantIndicatorReadDto> Indicators { get; set; } = [];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await ReloadRequired();
        NavService.OverridePageTitle(Tenant.Id.ToString(), Tenant.Name);
    }

    protected override async Task ReloadRequired()
    {
        Indicators.Clear();
        var indicatorsResult = await PartiesClient.GetTenantIndicatorsByTenantAsync(Tenant.Id);
        if (!indicatorsResult.IsSuccess || indicatorsResult.Value is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Error);
            Indicators = [];
            StateHasChanged();
            return;
        }

        Indicators = [.. indicatorsResult.Value];
        StateHasChanged();
    }

    protected async Task AddIndicator()
    {
        DialogParameters parameters = new()
        {
            {"TenantId", Tenant.Id }
        };
        IDialogReference dialog = await DialogService.ShowAsync<TenantIndicatorEditDialog>(null, parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        await ReloadRequired();
    }


    protected async Task EditIndicator(TenantIndicatorReadDto tenantIndicator)
    {
        DialogParameters parameters = new()
        {
            {"TenantId", Tenant.Id },
            {"TenantIndicator", tenantIndicator }
        };
        IDialogReference dialog = await DialogService.ShowAsync<TenantIndicatorEditDialog>(null, parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        await ReloadRequired();
    }

    private static string GetStatusIcon(IndicatorStatus status) => status switch
    {
        IndicatorStatus.PendingVerification => "fa-solid fa-seal-exclamation",
        IndicatorStatus.Verified => "fa-solid fa-check",
        IndicatorStatus.Active => "fa-solid fa-check-circle",
        _ => "fa-solid fa-pause"
    };
}
