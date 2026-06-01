namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class ProviderManagementGrid : PrescriptionBase
{
    private IReadOnlyList<ProviderManagementDto>? providers;

    protected override async Task OnInitializedAsync()
    {
        await LoadProvidersAsync();
    }

    private async Task LoadProvidersAsync()
    {
        providers = await Mediator.Send(new GetProviderManagementGridQuery());
        await InvokeAsync(StateHasChanged);
    }

    private async Task TestConnectivityAsync(string providerKey)
    {
        var result = await Mediator.Send(new TestProviderConnectivityCommand { ProviderKey = providerKey });
        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["ConnectivityTestPassed", result.LatencyMs], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }
    }

    private async Task SetStatusAsync(Guid providerId, ProviderStatus newStatus)
    {
        var result = await Mediator.Send(new SetProviderStatusCommand
        {
            ProviderId = providerId,
            NewStatus = newStatus
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["ProviderStatusUpdated"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadProvidersAsync();
    }

    private async Task ShowRegistrationDialogAsync()
    {
        var dialog = await DialogService.ShowAsync<ProviderRegistrationDialog>(
            Localizer["RegisterProvider"]);
        var dialogResult = await dialog.Result;

        if (dialogResult is not null && !dialogResult.Canceled)
        {
            await LoadProvidersAsync();
        }
    }

    private static Color GetProviderStatusColor(ProviderStatus status) => status switch
    {
        ProviderStatus.Active => Color.Success,
        ProviderStatus.Sandbox => Color.Info,
        ProviderStatus.Degraded => Color.Warning,
        ProviderStatus.Offline => Color.Error,
        ProviderStatus.Disabled => Color.Dark,
        _ => Color.Default
    };
}
