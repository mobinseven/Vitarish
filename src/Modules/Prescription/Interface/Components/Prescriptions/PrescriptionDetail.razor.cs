namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionDetail : PrescriptionBase
{
    [Parameter, EditorRequired] public PrescriptionDto Prescription { get; set; } = default!;
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private async Task OnItemAdded()
    {
        await ReloadPrescription();
    }

    private async Task OnItemRemoved()
    {
        await ReloadPrescription();
    }

    private async Task ValidateAndSubmitAsync()
    {
        ValidateDraftResult validation = await Mediator.Send(new ValidateDraftCommand
        {
            PrescriptionId = Prescription.Id
        });

        if (!validation.IsValid)
        {
            string errorMessage = string.Join(Environment.NewLine, validation.Errors);
            await DialogService.ShowMessageBox(
                Localizer["ValidationFailed"],
                errorMessage,
                yesText: Localizer["OK"]);
            return;
        }

        if (validation.Warnings.Count > 0)
        {
            string warningMessage = string.Join(Environment.NewLine, validation.Warnings);
            bool? proceed = await DialogService.ShowMessageBox(
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
            await DialogService.ShowMessageBox(
                Localizer["SubmissionFailed"],
                result.ErrorMessage ?? Localizer["UnknownError"],
                yesText: Localizer["OK"]);
            await ReloadPrescription();
        }
    }

    private async Task EditAsync()
    {
        string correlationId = Guid.NewGuid().ToString("N");
        EditPrescriptionResult result = await Mediator.Send(new EditPrescriptionCommand
        {
            PrescriptionId = Prescription.Id,
            CorrelationId = correlationId
        });

        if (!result.IsSuccess)
        {
            await DialogService.ShowMessageBox(
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
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmCancelPrescription"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["CancelItem", Localizer["Prescription"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(
            Localizer["CancelItem", Localizer["Prescription"]], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        CancelPrescriptionResult result = await Mediator.Send(new CancelPrescriptionCommand
        {
            PrescriptionId = Prescription.Id,
            CorrelationId = correlationId
        });

        if (!result.IsSuccess)
        {
            await DialogService.ShowMessageBox(
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
