using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Admins.Interface.Components.Admin;

public partial class TenantIndicatorEditDialog
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;
    [Parameter, EditorRequired] public Tenant Tenant { get; set; }
    [Parameter] public TenantIndicator? TenantIndicator { get; set; }

    private IndicatorMethod newMethod = IndicatorMethod.Hostname;
    private IndicatorVerificationMethod newVerificationMethod = IndicatorVerificationMethod.DnsTxt;
    private IndicatorStatus newStatus = IndicatorStatus.Disabled;
    private string newValue = string.Empty;
    private bool newIsPrimary;
    private string verificationToken = string.Empty;
    private string verificationEvidence = string.Empty;

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
            }
        }
    }

    private Task Save()
    {
        // TODO: Implement TenantIndicator management using EntityService pattern
        MudDialog.Cancel();
        return Task.CompletedTask;
    }
}
