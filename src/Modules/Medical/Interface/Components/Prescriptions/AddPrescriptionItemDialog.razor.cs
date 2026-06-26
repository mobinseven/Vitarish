using MudBlazor;

namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class AddPrescriptionItemDialog : PrescriptionBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter] public Guid PrescriptionId { get; set; }
    [Parameter] public string? ProviderKey { get; set; }

    [Inject] private IMedicalClient MedicalQuery { get; set; } = default!;

    private PrescriptionItemCategory category = PrescriptionItemCategory.Drug;
    private string? searchTerm;
    private int quantity = 1;
    private int durationDays;
    private (string Code, string DisplayName, string? Category)? selectedCode;
    private List<(string Code, string DisplayName, string? Category)> searchResults = [];

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

        string systemKey = string.IsNullOrEmpty(ProviderKey)
            ? codeSystem
            : $"{ProviderKey.ToLowerInvariant()}.{codeSystem}";

        IReadOnlyList<MedicalCode> results = await MedicalQuery.SearchMedicalCodesAsync(
            systemKey,
            searchTerm,
            limit: 20);

        searchResults = results
            .Select(c => (c.Code, c.DisplayName, Category: (string?)null))
            .ToList();

        await InvokeAsync(StateHasChanged);
    }

    private void SelectCode((string Code, string DisplayName, string? Category) code)
    {
        selectedCode = code;
        searchResults = [];
        StateHasChanged();
    }

    private async Task AddAsync()
    {
        AddItemResult result = await MedicalClient.Send(new AddItemCommand
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
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["Error"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
        }
    }

    private void Cancel() => MudDialog.Cancel();
}
