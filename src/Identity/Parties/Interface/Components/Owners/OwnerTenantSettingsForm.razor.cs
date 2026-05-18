namespace Vitarish.Parties.Interface.Components.Owners;

public partial class OwnerTenantSettingsForm
{
    [Parameter, EditorRequired] public TenantReadDto Tenant { get; set; } = default!;
    [Parameter] public EventCallback<TenantReadDto> TenantChanged { get; set; }

    private TenantWriteDto? write;

    protected override void OnParametersSet() => write = Tenant?.ToWriteWithMetadata();

    private async Task Save()
    {
        if (write is null)
            return;

        TenantReadDto? updated = await PartiesService.UpdateTenantAsync(write);
        if (updated is null)
        {
            Snackbar.Add(Localizer["Error"], Severity.Error);
            return;
        }

        await TenantChanged.InvokeAsync(updated);
        Snackbar.Add(Localizer["Saved"], Severity.Success);
    }
}
