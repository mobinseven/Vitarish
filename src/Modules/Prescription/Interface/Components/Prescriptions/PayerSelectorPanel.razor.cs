namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PayerSelectorPanel : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
    [Parameter] public EventCallback OnProviderChanged { get; set; }

    private List<PatientCoverageDto> coverages = [];
    private string? selectedProviderKey;
    private bool isLoading;
    private string? errorMessage;
    private Guid? loadedPatientId;

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Prescription is null)
            return;

        selectedProviderKey = Prescription.SelectedProviderKey;

        if (loadedPatientId != Prescription.PatientId)
        {
            isLoading = true;

            try
            {
                var result = await Mediator.Send(new GetPatientCoveragesQuery
                {
                    PatientId = Prescription.PatientId
                });

                loadedPatientId = Prescription.PatientId;
                coverages = [.. result];
            }
            finally
            {
                isLoading = false;
            }
        }

        // Auto-select if only one coverage and none selected
        if (string.IsNullOrEmpty(selectedProviderKey) && coverages.Count == 1)
        {
            await ApplyProviderSelectionAsync(coverages[0].ProviderKey, coverages[0].CoverageId);
        }
    }

    private async Task OnProviderSelectedAsync(string providerKey)
    {
        if (string.IsNullOrEmpty(providerKey) || providerKey == selectedProviderKey)
            return;

        var coverage = coverages.FirstOrDefault(c => c.ProviderKey == providerKey);
        await ApplyProviderSelectionAsync(providerKey, coverage?.CoverageId);
    }

    private async Task ApplyProviderSelectionAsync(string providerKey, Guid? coverageId)
    {
        errorMessage = null;

        SetProviderResult result = await Mediator.Send(new SetProviderCommand
        {
            PrescriptionId = Prescription.Id,
            ProviderKey = providerKey,
            PatientCoverageId = coverageId
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
