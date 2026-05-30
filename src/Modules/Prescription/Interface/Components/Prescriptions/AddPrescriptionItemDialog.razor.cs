namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class AddPrescriptionItemDialog : PrescriptionBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public Guid PrescriptionId { get; set; }
    [Parameter] public string? ProviderKey { get; set; }

    private PrescriptionItemCategory category = PrescriptionItemCategory.Drug;
    private string? searchTerm;
    private int quantity = 1;
    private int durationDays;
    private ReferenceCodeDto? selectedCode;
    private List<ReferenceCodeDto> searchResults = [];

    private bool CanAdd => selectedCode is not null || !string.IsNullOrEmpty(searchTerm);

    private async Task SearchCodesAsync()
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            searchResults = [];
            return;
        }

        string codeSystem = category switch
        {
            PrescriptionItemCategory.Drug => "drug",
            PrescriptionItemCategory.Lab => "lab",
            PrescriptionItemCategory.Imaging => "imaging",
            PrescriptionItemCategory.MedicalService => "service",
            _ => "general"
        };

        IReadOnlyList<ReferenceCodeDto> results = await Mediator.Send(new SearchReferenceCodesQuery
        {
            ProviderKey = ProviderKey ?? string.Empty,
            CodeSystem = codeSystem,
            SearchTerm = searchTerm,
            Take = 20
        });

        searchResults = [.. results];
        await InvokeAsync(StateHasChanged);
    }

    private void SelectCode(ReferenceCodeDto code)
    {
        selectedCode = code;
        searchResults = [];
        StateHasChanged();
    }

    private async Task AddAsync()
    {
        AddItemResult result = await Mediator.Send(new AddItemCommand
        {
            PrescriptionId = PrescriptionId,
            Category = category,
            ReferenceCode = selectedCode?.Code,
            DisplayName = selectedCode?.DisplayName ?? searchTerm,
            Quantity = quantity,
            DurationDays = durationDays > 0 ? durationDays : null
        });

        if (result.IsSuccess)
        {
            MudDialog.Close(DialogResult.Ok(result.ItemId));
        }
        else
        {
            await DialogService.ShowMessageBox(
                Localizer["Error"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
