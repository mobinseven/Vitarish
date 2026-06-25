namespace Vitarish.Parties.Interface.Components.Owners;

public partial class OwnerTenantSettingsForm
{
    [Parameter, EditorRequired] public TenantReadDto Tenant { get; set; } = default!;
    [Parameter] public EventCallback<TenantReadDto> TenantChanged { get; set; }

    private TenantWriteDto? write;

    protected override void OnParametersSet() => write = Tenant is null ? null : new TenantWriteDto
    {
        Id = Tenant.Id,
        OwnerId = Tenant.OwnerId,
        Name = Tenant.Name,
        LogoUrl = Tenant.LogoUrl,
        Description = Tenant.Description,
        AppId = Tenant.AppId,
        Metadata = Tenant.Metadata is null ? [] : new Dictionary<string, object?>(Tenant.Metadata)
    };

    private async Task Save()
    {
        if (write is null)
        {
            return;
        }

        var result = await PartiesClient.UpdateTenantAsync(write, CancellationToken.None);
        if (!result.IsSuccess || result.Value is null)
        {
            _ = Snackbar.Add(result.Error?.Message ?? Localizer["Error"], Severity.Error);
            return;
        }

        await TenantChanged.InvokeAsync(result.Value);
        _ = Snackbar.Add(Localizer["Saved"], Severity.Success);
    }
}
