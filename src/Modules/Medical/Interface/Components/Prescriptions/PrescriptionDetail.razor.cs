using MudBlazor;
namespace Vitarish.Medical.Interface.Components.Prescriptions;

public partial class PrescriptionDetail : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private async Task OnItemAdded() => await ReloadPrescription();

    private async Task OnItemRemoved() => await ReloadPrescription();

    private async Task OnProviderChangedAsync() => await ReloadPrescription();

    private async Task ValidateAndSubmitAsync()
    {
        ValidateDraftResult validation = await Mediator.Send(new ValidateDraftCommand
        {
            PrescriptionId = Prescription.Id
        });

        if (!validation.IsValid)
        {
            string errorMessage = string.Join(Environment.NewLine, validation.Errors);
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["ValidationFailed"],
                errorMessage,
                yesText: Localizer["OK"]);
            return;
        }

        if (validation.Warnings.Count > 0)
        {
            string warningMessage = string.Join(Environment.NewLine, validation.Warnings);
            bool? proceed = await DialogService.ShowMessageBoxAsync(
                Localizer["Warnings"],
                warningMessage,
                yesText: Localizer["Proceed"],
                cancelText: Localizer["Cancel"]);

            if (proceed != true)
            {
                return;
            }
        }

        string correlationId = Guid.NewGuid().ToString("N");
        SubmitPrescriptionResult result = await Mediator.Send(new SubmitPrescriptionCommand
        {
            PrescriptionId = Prescription.Id,
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            await ReloadPrescription();
        }
        else
        {
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["SubmissionFailed"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
            await ReloadPrescription();
        }
    }

    private async Task EditAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmEditPrescription"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["EditItem", Localizer["Prescription"]].Value },
            { nameof(ConfirmDialog.Color), Color.Info }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["EditItem", Localizer["Prescription"]], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        EditPrescriptionResult result = await Mediator.Send(new EditPrescriptionCommand
        {
            PrescriptionId = Prescription.Id,
            CorrelationId = correlationId,
            EditReason = dialogResult.Data?.ToString()
        });

        if (!result.IsSuccess)
        {
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["EditFailed"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
        }

        await ReloadPrescription();
    }

    private async Task CancelAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(CancelReasonDialog.PrescriptionId), Prescription.Id }
        };

        IDialogReference dialog = await DialogService.ShowAsync<CancelReasonDialog>(
            Localizer["CancelItem", Localizer["Prescription"]], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string? reason = dialogResult.Data?.ToString();
        string correlationId = Guid.NewGuid().ToString("N");
        CancelPrescriptionResult result = await Mediator.Send(new CancelPrescriptionCommand
        {
            PrescriptionId = Prescription.Id,
            CorrelationId = correlationId,
            Reason = reason
        });

        if (!result.IsSuccess)
        {
            _ = await DialogService.ShowMessageBoxAsync(
                Localizer["CancelFailed"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
        }

        await ReloadPrescription();
    }

    private async Task ReloadPrescription()
    {
        PrescriptionDto? updated = await Mediator.Send(new GetPrescriptionQuery
        {
            PrescriptionId = Prescription.Id
        });

        if (updated is not null)
        {
            Prescription = updated;
        }

        await OnStatusChanged.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }
}
