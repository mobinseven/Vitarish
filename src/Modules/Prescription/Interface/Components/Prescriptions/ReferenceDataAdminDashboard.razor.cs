namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class ReferenceDataAdminDashboard : PrescriptionBase
{
    private IReadOnlyList<ReferenceFreshnessDto>? freshnessData;
    private IReadOnlyList<ReferenceSyncJobDto>? recentJobs;
    private IReadOnlyList<ReferenceChangeLogDto>? breakingChanges;
    private ReferenceCodeCrosswalkGridResult? crosswalkGrid;

    private string? selectedProviderKey;
    private bool isSyncing;
    private string activeTab = "freshness";

    protected override async Task OnInitializedAsync()
    {
        await LoadFreshnessAsync();
        await LoadRecentJobsAsync();
        await LoadBreakingChangesAsync();
    }

    private async Task LoadFreshnessAsync()
    {
        freshnessData = await Mediator.Send(new GetReferenceFreshnessQuery
        {
            ProviderKey = selectedProviderKey
        });
    }

    private async Task LoadRecentJobsAsync()
    {
        recentJobs = await Mediator.Send(new GetReferenceSyncJobHistoryQuery
        {
            ProviderKey = selectedProviderKey,
            Take = 10
        });
    }

    private async Task LoadBreakingChangesAsync()
    {
        breakingChanges = await Mediator.Send(new GetReferenceChangeLogsQuery
        {
            ProviderKey = selectedProviderKey,
            BreakingOnly = true,
            Take = 20
        });
    }

    private async Task LoadCrosswalkGridAsync()
    {
        crosswalkGrid = await Mediator.Send(new GetReferenceCodeCrosswalkGridQuery
        {
            ProviderKey = selectedProviderKey,
            Take = 50
        });
    }

    private async Task OnProviderFilterChanged(string? value)
    {
        selectedProviderKey = value;
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await LoadFreshnessAsync();
        await LoadRecentJobsAsync();
        await LoadBreakingChangesAsync();
        if (activeTab == "crosswalks")
            await LoadCrosswalkGridAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task TriggerSyncAsync(string providerKey, string codeSystem)
    {
        isSyncing = true;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await Mediator.Send(new SyncReferenceDataCommand
            {
                ProviderKey = providerKey,
                CodeSystem = codeSystem,
                Priority = Domain.Enums.SyncPriority.High,
                TriggeredBy = "admin:manual"
            });

            if (result.IsSuccess)
            {
                Snackbar.Add(
                    Localizer["SyncCompleted", result.Changes?.Added ?? 0, result.Changes?.Updated ?? 0, result.Changes?.Deactivated ?? 0],
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
            await RefreshAllAsync();
        }
    }

    private async Task OnTabChanged(string tab)
    {
        activeTab = tab;
        if (tab == "crosswalks" && crosswalkGrid is null)
            await LoadCrosswalkGridAsync();
    }

    private async Task DeactivateCrosswalkAsync(Guid crosswalkId)
    {
        var result = await Mediator.Send(new DeactivateReferenceCodeCrosswalkCommand
        {
            CrosswalkId = crosswalkId
        });

        if (result)
        {
            Snackbar.Add(Localizer["CrosswalkDeactivated"], Severity.Success);
            await LoadCrosswalkGridAsync();
            await InvokeAsync(StateHasChanged);
        }
        else
        {
            Snackbar.Add(Localizer["CrosswalkDeactivationFailed"], Severity.Error);
        }
    }

    private static string FormatDuration(long ms) =>
        ms < 1000 ? $"{ms}ms" : $"{ms / 1000.0:F1}s";

    private static Color GetFreshnessColor(ReferenceFreshnessDto item) =>
        item.IsStale ? Color.Warning :
        item.RecentBreakingChanges > 0 ? Color.Error :
        Color.Success;

    private static string GetSyncStatusIcon(ReferenceCodeSyncStatus status) => status switch
    {
        ReferenceCodeSyncStatus.Completed => Icons.Material.Filled.CheckCircle,
        ReferenceCodeSyncStatus.CompletedWithErrors => Icons.Material.Filled.Warning,
        ReferenceCodeSyncStatus.Failed => Icons.Material.Filled.Error,
        ReferenceCodeSyncStatus.InProgress => Icons.Material.Filled.Sync,
        _ => Icons.Material.Filled.HourglassEmpty
    };
}
