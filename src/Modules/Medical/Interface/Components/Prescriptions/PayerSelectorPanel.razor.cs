using MudBlazor;
namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PayerSelectorPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
    [Parameter] public EventCallback OnProviderChanged { get; set; }

    private List<AvailableProviderDto> providers = [];
    private string? selectedProviderKey;
    private bool isLoading;
    private string? errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Prescription is null)
        {
            return;
        }

        selectedProviderKey = Prescription.SelectedProviderKey;

        if (providers.Count == 0)
        {
            isLoading = true;

            try
            {
                IReadOnlyList<AvailableProviderDto> result = await MedicalClient.Send(new GetAvailableProvidersQuery());
                providers = [.. result];
            }
            finally
            {
                isLoading = false;
            }
        }

        // Auto-select if only one provider and none selected, and no previous error
        if (string.IsNullOrEmpty(selectedProviderKey) && providers.Count == 1 && string.IsNullOrEmpty(errorMessage))
        {
            await ApplyProviderSelectionAsync(providers[0].ProviderKey);
        }
    }

    private async Task OnProviderSelectedAsync(string providerKey)
    {
        if (string.IsNullOrEmpty(providerKey) || providerKey == selectedProviderKey)
        {
            return;
        }

        await ApplyProviderSelectionAsync(providerKey);
    }

    private async Task ApplyProviderSelectionAsync(string providerKey)
    {
        errorMessage = null;

        SetProviderResult result = await MedicalClient.Send(new SetProviderCommand
        {
            PrescriptionId = Prescription.Id,
            ProviderKey = providerKey
        });

        if (result.IsSuccess)
        {
            selectedProviderKey = providerKey;
            await OnProviderChanged.InvokeAsync();
        }
        else
        {
            errorMessage = result.ErrorMessage;
        }

        await InvokeAsync(StateHasChanged);
    }
}
