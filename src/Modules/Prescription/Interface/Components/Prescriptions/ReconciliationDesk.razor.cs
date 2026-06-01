namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class ReconciliationDesk : PrescriptionBase
{
    private ReconciliationQueueResult? queueResult;
    private PrescriptionStatus? statusFilter;

    protected override async Task OnInitializedAsync()
    {
        await LoadQueueAsync();
    }

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
        var correlationId = Guid.NewGuid().ToString("N");
        var result = await Mediator.Send(new RefreshProviderStatusCommand
        {
            PrescriptionId = prescriptionId,
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["StatusRefreshed", result.NewStatus?.ToString() ?? ""], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadQueueAsync();
    }

    private async Task ResolveDuplicateRiskAsync(Guid prescriptionId, DuplicateRiskResolution resolution)
    {
        var parameters = new DialogParameters
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmDuplicateRiskResolution"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Confirm"].Value },
            { nameof(ConfirmDialog.Color), Color.Warning }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ResolveDuplicateRisk"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled) return;

        var correlationId = Guid.NewGuid().ToString("N");
        var result = await Mediator.Send(new ResolveDuplicateRiskCommand
        {
            PrescriptionId = prescriptionId,
            Resolution = resolution,
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["DuplicateRiskResolved"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadQueueAsync();
    }

    private async Task ManualReconcileAsync(Guid prescriptionId)
    {
        var parameters = new DialogParameters
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmManualReconcile"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["ManualReconcile"].Value },
            { nameof(ConfirmDialog.Color), Color.Tertiary }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ManualReconcile"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled) return;

        var correlationId = Guid.NewGuid().ToString("N");
        var result = await Mediator.Send(new ManualReconcileCommand
        {
            PrescriptionId = prescriptionId,
            Notes = "Manual reconciliation by support operator",
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["ManuallyReconciled"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadQueueAsync();
    }

    private async Task ReconcilePaperFallbackAsync(Guid prescriptionId)
    {
        var parameters = new DialogParameters
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmPaperFallbackReconcile"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Reconcile"].Value },
            { nameof(ConfirmDialog.Color), Color.Success }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["ReconcilePaperFallback"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled) return;

        var correlationId = Guid.NewGuid().ToString("N");
        var result = await Mediator.Send(new ReconcilePaperFallbackCommand
        {
            PrescriptionId = prescriptionId,
            CorrelationId = correlationId
        });

        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["PaperFallbackReconciled"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadQueueAsync();
    }

    private static string FormatItemLabel(ReconciliationItemDto item) =>
        $"{item.CreatedAt.ToLocalTime():g} — {item.SelectedProviderKey ?? "—"} ({item.Status})";
}
