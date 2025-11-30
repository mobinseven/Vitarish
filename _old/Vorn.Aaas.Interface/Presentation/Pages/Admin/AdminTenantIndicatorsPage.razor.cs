using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Presentation.Contracts;

namespace Vorn.Aaas.Interface.Presentation.Pages.Admin;

public partial class AdminTenantIndicatorsPage
{
    List<Tenant> Tenants { get; set; } = [];
    List<TenantIndicator> Indicators { get; set; } = [];
    Guid selectedTenantId = Guid.Empty;
    IndicatorMethod newMethod = IndicatorMethod.Hostname;
    IndicatorVerificationMethod newVerificationMethod = IndicatorVerificationMethod.DnsTxt;
    string newValue = string.Empty;
    bool newIsPrimary;
    string feedbackMessage = string.Empty;
    Severity feedbackSeverity = Severity.Success;
    string verificationToken = string.Empty;
    string verificationEvidence = string.Empty;

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

    async Task LoadIndicators()
    {
        if (selectedTenantId != Guid.Empty)
        {
            Indicators = [.. await PartiesService.GetTenantIndicatorsAsync(selectedTenantId)];
            StateHasChanged();
        }
    }

    async Task AddIndicator()
    {
        if (selectedTenantId == Guid.Empty || string.IsNullOrWhiteSpace(newValue))
        {
            ShowFeedback("Please select a tenant and provide a valid value.", Severity.Warning);
            return;
        }

        try
        {
            var request = new AddTenantIndicatorRequest(selectedTenantId, newMethod, newValue.Trim(), newIsPrimary, newVerificationMethod);
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

    async Task VerifyIndicator(TenantIndicator indicator)
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

    async Task ActivateIndicator(TenantIndicator indicator)
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

    async Task DisableIndicator(TenantIndicator indicator)
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

    async Task DeleteIndicator(TenantIndicator indicator)
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

    Color GetStatusColor(IndicatorStatus status) => status switch
    {
        IndicatorStatus.PendingVerification => Color.Warning,
        IndicatorStatus.Verified => Color.Info,
        IndicatorStatus.Active => Color.Success,
        IndicatorStatus.Disabled => Color.Default,
        _ => Color.Default
    };

    void ShowFeedback(string message, Severity severity)
    {
        feedbackMessage = message;
        feedbackSeverity = severity;
        StateHasChanged();
    }

    void ShowDnsInstructions(TenantIndicator indicator)
    {
        if (indicator.Verification is not null)
        {
            verificationEvidence = indicator.Verification.Evidence;
            verificationToken = indicator.Verification.Token ?? string.Empty;
            ShowFeedback($"DNS TXT record instructions displayed for '{indicator.Value}'", Severity.Info);
        }
    }
}
