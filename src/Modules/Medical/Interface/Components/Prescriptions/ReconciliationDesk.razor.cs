namespace Vitarish.Medical.Interface.Components.Prescriptions.Prescriptions;

public partial class ReconciliationDesk : PrescriptionBase
{
    private ReconciliationQueueResult? queueResult;
    private PrescriptionStatus? statusFilter;

    protected override async Task OnInitializedAsync() => await LoadQueueAsync();

    private async Task OnStatusFilterChanged(PrescriptionStatus? value)
    {
        statusFilter = value;
        await LoadQueueAsync();
    }

    private async Task LoadQueueAsync()
    {
        queueResult = await Mediator.Send(new GetReconciliationQueueQuery
        {
            StatusFilter = statusFilter,
            Take = 50
        });
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshStatusAsync(Guid prescriptionId)
    {
        string correlationId = Guid.NewGuid().ToString("N");
        RefreshProviderStatusResult result = await Mediator.Send(new RefreshProviderStatusCommand
        {
            PrescriptionId = prescriptionId,
            CorrelationId = correlationId
        });

        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["StatusRefreshed", result.NewStatus?.ToString() ?? ""], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadQueueAsync();
    }

    private async Task ResolveDuplicateRiskAsync(Guid prescriptionId, DuplicateRiskResolution resolution)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmDuplicateRiskResolution"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Confirm"].Value },
            { nameof(ConfirmDialog.Color), Color.Warning }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ResolveDuplicateRisk"], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        ResolveDuplicateRiskResult result = await Mediator.Send(new ResolveDuplicateRiskCommand
        {
            PrescriptionId = prescriptionId,
            Resolution = resolution,
            CorrelationId = correlationId
        });

        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["DuplicateRiskResolved"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadQueueAsync();
    }

    private async Task ManualReconcileAsync(Guid prescriptionId)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmManualReconcile"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["ManualReconcile"].Value },
            { nameof(ConfirmDialog.Color), Color.Tertiary }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ManualReconcile"], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string notes = dialogResult.Data?.ToString() ?? "Manual reconciliation by support operator";
        if (string.IsNullOrWhiteSpace(notes))
        {
            notes = "Manual reconciliation by support operator";
        }

        string correlationId = Guid.NewGuid().ToString("N");
        ManualReconcileResult result = await Mediator.Send(new ManualReconcileCommand
        {
            PrescriptionId = prescriptionId,
            Notes = notes,
            CorrelationId = correlationId
        });

        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["ManuallyReconciled"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadQueueAsync();
    }

    private async Task EscalateAsync(Guid prescriptionId)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmEscalation"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Escalate"].Value },
            { nameof(ConfirmDialog.Color), Color.Secondary }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["Escalate"], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        EscalateResult result = await Mediator.Send(new EscalateCommand
        {
            PrescriptionId = prescriptionId,
            OperatorId = Guid.Empty, // Will be populated from auth context
            Summary = dialogResult.Data?.ToString() ?? "Escalated by support operator",
            CorrelationId = correlationId
        });

        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["Escalated"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadQueueAsync();
    }

    private async Task ReconcilePaperFallbackAsync(Guid prescriptionId)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmPaperFallbackReconcile"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Reconcile"].Value },
            { nameof(ConfirmDialog.Color), Color.Success }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ReconcilePaperFallback"], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        string correlationId = Guid.NewGuid().ToString("N");
        ReconcilePaperFallbackResult result = await Mediator.Send(new ReconcilePaperFallbackCommand
        {
            PrescriptionId = prescriptionId,
            CorrelationId = correlationId
        });

        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["PaperFallbackReconciled"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadQueueAsync();
    }

    private static string FormatItemLabel(ReconciliationItemDto item) =>
        $"{item.CreatedAt.ToLocalTime():g} — {item.SelectedProviderKey ?? "—"} ({item.Status})";
}
