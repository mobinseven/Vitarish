namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class PrescriptionOperationalDashboard : PrescriptionBase
{
    private IReadOnlyList<ProviderTransactionMetricsDto>? transactionMetrics;
    private IReadOnlyList<RejectionDistributionDto>? rejectionDistribution;
    private CredentialHealthResult? credentialHealth;
    private IReadOnlyList<OperationalAlertDto>? alerts;
    private DuplicateRiskIncidentsResult? duplicateRiskResult;
    private OutboxStatusResult? outboxStatus;

    private string? selectedProviderKey;

    protected override async Task OnInitializedAsync()
    {
        await LoadAlertsAsync();
        await LoadTransactionMetricsAsync();
    }

    private async Task OnProviderFilterChanged(string? value)
    {
        selectedProviderKey = value;
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await LoadAlertsAsync();
        await LoadTransactionMetricsAsync();
        await LoadRejectionDistributionAsync();
        if (credentialHealth is not null)
        {
            await LoadCredentialHealthAsync();
        }

        if (duplicateRiskResult is not null)
        {
            await LoadDuplicateRiskAsync();
        }

        if (outboxStatus is not null)
        {
            await LoadOutboxStatusAsync();
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task OnTabChanged(string tab)
    {
        switch (tab)
        {
            case AlertsTab when alerts is null:
                await LoadAlertsAsync();
                break;
            case TransactionsTab when transactionMetrics is null:
                await LoadTransactionMetricsAsync();
                break;
            case RejectionsTab when rejectionDistribution is null:
                await LoadRejectionDistributionAsync();
                break;
            case CredentialsTab when credentialHealth is null:
                await LoadCredentialHealthAsync();
                break;
            case DuplicateRiskTab when duplicateRiskResult is null:
                await LoadDuplicateRiskAsync();
                break;
            case RetryQueueTab when outboxStatus is null:
                await LoadOutboxStatusAsync();
                break;
        }
    }

    private async Task LoadAlertsAsync() => alerts = await Mediator.Send(new GetOperationalAlertsQuery
    {
        ProviderKey = selectedProviderKey
    });

    private async Task LoadTransactionMetricsAsync() => transactionMetrics = await Mediator.Send(new GetProviderTransactionMetricsQuery
    {
        ProviderKey = selectedProviderKey,
        From = DateTime.UtcNow.AddDays(-7)
    });

    private async Task LoadRejectionDistributionAsync() => rejectionDistribution = await Mediator.Send(new GetRejectionDistributionQuery
    {
        ProviderKey = selectedProviderKey,
        From = DateTime.UtcNow.AddDays(-30)
    });

    private async Task LoadCredentialHealthAsync() => credentialHealth = await Mediator.Send(new GetCredentialHealthQuery
    {
        ProviderKey = selectedProviderKey
    });

    private async Task LoadDuplicateRiskAsync() => duplicateRiskResult = await Mediator.Send(new GetDuplicateRiskIncidentsQuery
    {
        ProviderKey = selectedProviderKey,
        Take = 20
    });

    private async Task LoadOutboxStatusAsync() => outboxStatus = await Mediator.Send(new GetOutboxStatusQuery
    {
        ProviderKey = selectedProviderKey
    });

    private static Color GetAlertColor(string severity) => severity switch
    {
        "Critical" => Color.Error,
        "Warning" => Color.Warning,
        "Info" => Color.Info,
        _ => Color.Default
    };

    private static Color GetSuccessRateColor(double rate) => rate switch
    {
        >= 95 => Color.Success,
        >= 80 => Color.Warning,
        _ => Color.Error
    };

    private static Color GetCredentialStatusColor(CredentialStatus status) => status switch
    {
        CredentialStatus.Active => Color.Success,
        CredentialStatus.Expired => Color.Error,
        CredentialStatus.Revoked => Color.Dark,
        CredentialStatus.Pending => Color.Warning,
        _ => Color.Default
    };
}
