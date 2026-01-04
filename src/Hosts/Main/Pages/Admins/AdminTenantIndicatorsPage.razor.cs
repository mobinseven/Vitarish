using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Main.Pages.Admins;

public partial class AdminTenantIndicatorsPage
{
    private List<Tenant> Tenants { get; set; } = [];
    private List<TenantIndicator> Indicators { get; set; } = [];
    private Guid selectedTenantId = Guid.Empty;
    private IndicatorMethod newMethod = IndicatorMethod.Hostname;
    private IndicatorVerificationMethod newVerificationMethod = IndicatorVerificationMethod.DnsTxt;
    private string newValue = string.Empty;
    private bool newIsPrimary;
    private string feedbackMessage = string.Empty;
    private Severity feedbackSeverity = Severity.Success;
    private string verificationToken = string.Empty;
    private string verificationEvidence = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        Tenants = [.. await PartiesService.GetTenantsAsync()];
        if (Tenants.Count > 0)
        {
            selectedTenantId = Tenants[0].Id;
            await LoadIndicators();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (selectedTenantId != Guid.Empty)
        {
            await LoadIndicators();
        }
    }

    private async Task LoadIndicators()
    {
        if (selectedTenantId != Guid.Empty)
        {
            Indicators = [.. await PartiesService.GetTenantIndicatorsAsync(selectedTenantId)];
            StateHasChanged();
        }
    }

    private async Task AddIndicator()
    {
        if (selectedTenantId == Guid.Empty || string.IsNullOrWhiteSpace(newValue))
        {
            ShowFeedback("Please select a tenant and provide a valid value.", Severity.Warning);
            return;
        }

        try
        {
            AddTenantIndicator request = new(selectedTenantId, newMethod, newValue.Trim(), newIsPrimary, newVerificationMethod);
            TenantIndicator? indicator = await PartiesService.AddTenantIndicatorAsync(request);

            if (indicator is not null)
            {
                Indicators.Add(indicator);

                // Clear previous verification display
                verificationToken = string.Empty;
                verificationEvidence = string.Empty;

                // Show verification instructions if DNS TXT method is used
                if (newVerificationMethod == IndicatorVerificationMethod.DnsTxt && indicator.Verification is not null)
                {
                    verificationEvidence = indicator.Verification.Evidence;
                    verificationToken = indicator.Verification.Token ?? string.Empty;
                    ShowFeedback($"Indicator '{indicator.Value}' added. Please add the DNS TXT record shown below to verify.", Severity.Info);
                }
                else
                {
                    ShowFeedback($"Indicator '{indicator.Value}' added successfully. Status: {indicator.Status}", Severity.Success);
                }

                newValue = string.Empty;
                newIsPrimary = false;
            }
            else
            {
                ShowFeedback("Failed to add indicator.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error: {ex.Message}", Severity.Error);
        }

        StateHasChanged();
    }

    private async Task VerifyIndicator(TenantIndicator indicator)
    {
        try
        {
            bool result = await PartiesService.VerifyTenantIndicatorAsync(selectedTenantId, indicator.Id);
            if (result)
            {
                await LoadIndicators();
                ShowFeedback($"Indicator '{indicator.Value}' verified successfully.", Severity.Success);
            }
            else
            {
                ShowFeedback("Verification failed.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task ActivateIndicator(TenantIndicator indicator)
    {
        try
        {
            bool result = await PartiesService.ActivateTenantIndicatorAsync(selectedTenantId, indicator.Id);
            if (result)
            {
                await LoadIndicators();
                ShowFeedback($"Indicator '{indicator.Value}' activated successfully.", Severity.Success);
            }
            else
            {
                ShowFeedback("Activation failed.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task DisableIndicator(TenantIndicator indicator)
    {
        try
        {
            bool result = await PartiesService.DisableTenantIndicatorAsync(selectedTenantId, indicator.Id);
            if (result)
            {
                await LoadIndicators();
                ShowFeedback($"Indicator '{indicator.Value}' disabled successfully.", Severity.Success);
            }
            else
            {
                ShowFeedback("Disable failed.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteIndicator(TenantIndicator indicator)
    {
        try
        {
            bool result = await PartiesService.DeleteTenantIndicatorAsync(selectedTenantId, indicator.Id);
            if (result)
            {
                _ = Indicators.Remove(indicator);
                ShowFeedback($"Indicator '{indicator.Value}' deleted successfully.", Severity.Success);
                StateHasChanged();
            }
            else
            {
                ShowFeedback("Delete failed.", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            ShowFeedback($"Error: {ex.Message}", Severity.Error);
        }
    }

    private Color GetStatusColor(IndicatorStatus status) => status switch
    {
        IndicatorStatus.PendingVerification => Color.Warning,
        IndicatorStatus.Verified => Color.Info,
        IndicatorStatus.Active => Color.Success,
        IndicatorStatus.Disabled => Color.Default,
        _ => Color.Default
    };

    private void ShowFeedback(string message, Severity severity)
    {
        feedbackMessage = message;
        feedbackSeverity = severity;
        StateHasChanged();
    }

    private void ShowDnsInstructions(TenantIndicator indicator)
    {
        if (indicator.Verification is not null)
        {
            verificationEvidence = indicator.Verification.Evidence;
            verificationToken = indicator.Verification.Token ?? string.Empty;
            ShowFeedback($"DNS TXT record instructions displayed for '{indicator.Value}'", Severity.Info);
        }
    }
}
