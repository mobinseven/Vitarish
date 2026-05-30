namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionItemsGrid : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
    [Parameter] public EventCallback OnItemAdded { get; set; }
    [Parameter] public EventCallback OnItemRemoved { get; set; }

    private bool CanEdit => Prescription.Status is PrescriptionStatus.Draft or PrescriptionStatus.NeedsCorrection;

    private async Task RemoveItemAsync(Guid itemId)
    {
        RemoveItemResult result = await Mediator.Send(new RemoveItemCommand
        {
            PrescriptionId = Prescription.Id,
            ItemId = itemId
        });

        if (result.IsSuccess)
        {
            await OnItemRemoved.InvokeAsync();
        }
        else
        {
            await DialogService.ShowMessageBox(
                Localizer["Error"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
        }
    }

    private async Task ShowAddItemDialogAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(AddPrescriptionItemDialog.PrescriptionId), Prescription.Id },
            { nameof(AddPrescriptionItemDialog.ProviderKey), Prescription.SelectedProviderKey }
        };

        IDialogReference dialog = await DialogService.ShowAsync<AddPrescriptionItemDialog>(
            Localizer["AddItem", Localizer["Item"]], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is not null && !dialogResult.Canceled)
        {
            await OnItemAdded.InvokeAsync();
        }
    }

    private static string FormatItemLabel(PrescriptionItemDto item) =>
        !string.IsNullOrEmpty(item.DisplayName)
            ? $"{item.DisplayName} × {item.Quantity}"
            : $"{item.Category} — {item.ReferenceCode} × {item.Quantity}";

    private static Color CategoryColor(PrescriptionItemCategory category) => category switch
    {
        PrescriptionItemCategory.Drug => Color.Primary,
        PrescriptionItemCategory.Lab => Color.Info,
        PrescriptionItemCategory.Imaging => Color.Secondary,
        PrescriptionItemCategory.Visit => Color.Success,
        PrescriptionItemCategory.Referral => Color.Warning,
        _ => Color.Default
    };
}
