namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class ReferenceDataAdminDashboard : PrescriptionBase
{
    private const string DrugCodeSystem = "drug";
    private const string ServiceCodeSystem = "service";

    private IReadOnlyList<AvailableProviderDto>? availableProviders;
    private bool isSyncing;

    protected override async Task OnInitializedAsync()
    {
        availableProviders = await Mediator.Send(new GetAvailableProvidersQuery());
    }

    private async Task TriggerSyncAsync(string providerKey, string codeSystem)
    {
        isSyncing = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            SyncReferenceDataResult result = await Mediator.Send(new SyncReferenceDataCommand
            {
                ProviderKey = providerKey,
                CodeSystem = codeSystem,
                TriggeredBy = "admin:manual"
            });

            if (result.IsSuccess)
            {
                Snackbar.Add(
                    Localizer["SyncCompleted", result.TotalItemsProcessed ?? 0],
                    Severity.Success);
            }
            else
            {
                Snackbar.Add(result.ErrorMessage ?? Localizer["SyncFailed"], Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            isSyncing = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}
