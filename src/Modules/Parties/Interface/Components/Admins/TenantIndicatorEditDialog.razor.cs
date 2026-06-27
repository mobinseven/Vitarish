using Vitarish.Parties.Contracts.Tenants;

namespace Vitarish.Parties.Interface.Components.Admins;

public partial class TenantIndicatorEditDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter, EditorRequired] public Guid TenantId { get; set; }
    [Parameter] public TenantIndicatorReadDto? TenantIndicator { get; set; }

    private IndicatorMethod newMethod = IndicatorMethod.Hostname;
    private IndicatorVerificationMethod newVerificationMethod = IndicatorVerificationMethod.DnsTxt;
    private IndicatorStatus newStatus = IndicatorStatus.Disabled;
    private string newValue = string.Empty;
    private bool newIsPrimary;
    private string verificationToken = string.Empty;
    private string verificationEvidence = string.Empty;
    private string verificationExpectedHash = string.Empty;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (TenantIndicator is not null)
        {
            newValue = TenantIndicator.Value;
            newMethod = TenantIndicator.Method;
            newIsPrimary = TenantIndicator.IsPrimary;
            newStatus = TenantIndicator.Status;
            if (TenantIndicator.Verification is not null)
            {
                newVerificationMethod = TenantIndicator.Verification.Method;
                verificationEvidence = TenantIndicator.Verification.Evidence;
                verificationToken = TenantIndicator.Verification.Token ?? string.Empty;
                verificationExpectedHash = TenantIndicator.Verification.EvidenceExpectedHash;
            }
        }
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(newValue))
        {
            _ = Snackbar.Add("Value is required", Severity.Error);
            return;
        }

        VerificationInfoDto? verification = null;
        if (!string.IsNullOrWhiteSpace(verificationEvidence) && !string.IsNullOrWhiteSpace(verificationExpectedHash))
        {
            verification = new VerificationInfoDto
            {
                Method = newVerificationMethod,
                Evidence = verificationEvidence,
                EvidenceExpectedHash = verificationExpectedHash,
                Token = string.IsNullOrWhiteSpace(verificationToken) ? null : verificationToken
            };
        }

        if (TenantIndicator is null)
        {
            // Create new indicator
            TenantIndicatorWriteDto dto = new()
            {
                Id = Guid.NewGuid(),
                TenantId = TenantId,
                Method = newMethod,
                Value = newValue,
                Status = newStatus,
                IsPrimary = newIsPrimary,
                Verification = verification
            };
            var createdResult = await PartiesClient.CreateTenantIndicatorAsync(dto);
            TenantIndicatorReadDto? created = createdResult.IsSuccess ? createdResult.Value : null;
            if (created is not null)
            {
                _ = Snackbar.Add("Indicator created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(created));
            }
            else
            {
                _ = Snackbar.Add("Failed to create indicator", Severity.Error);
            }
        }
        else
        {
            // Update existing indicator
            TenantIndicatorWriteDto dto = new()
            {
                Id = TenantIndicator.Id,
                TenantId = TenantId,
                Method = newMethod,
                Value = newValue,
                Status = newStatus,
                IsPrimary = newIsPrimary,
                Verification = verification,
                VerifiedAt = TenantIndicator.VerifiedAt,
                LastCheckedAt = TenantIndicator.LastCheckedAt
            };
            var updatedResult = await PartiesClient.UpdateTenantIndicatorAsync(dto);
            TenantIndicatorReadDto? updated = updatedResult.IsSuccess ? updatedResult.Value : null;
            if (updated is not null)
            {
                _ = Snackbar.Add("Indicator updated successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(updated));
            }
            else
            {
                _ = Snackbar.Add("Failed to update indicator", Severity.Error);
            }
        }
    }

    private async Task Delete()
    {
        if (TenantIndicator is null)
        {
            MudDialog.Close();
        }
        else
        {
            var deletedResult = await PartiesClient.DeleteTenantIndicatorAsync(TenantIndicator.Id);
            if (!deletedResult.IsSuccess || !deletedResult.Value)
            {
                _ = Snackbar.Add("Failed to delete indicator", Severity.Error);
                return;
            }

            _ = Snackbar.Add("Indicator deleted successfully", Severity.Success);
            MudDialog.Close();
        }

    }
}
