namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class CredentialSetupGrid : PrescriptionBase
{
    private IReadOnlyList<CredentialSetupDto>? credentials;

    protected override async Task OnInitializedAsync()
    {
        await LoadCredentialsAsync();
    }

    private async Task LoadCredentialsAsync()
    {
        Guid? clinicId = Guid.TryParse(RequestContext.TenantId, out Guid cId) ? cId : null;
        if (clinicId is null) return;

        credentials = await Mediator.Send(new GetCredentialSetupGridQuery
        {
            ClinicId = clinicId.Value
        });
        await InvokeAsync(StateHasChanged);
    }

    private async Task TestCredentialAsync(Guid credentialId)
    {
        var result = await Mediator.Send(new TestCredentialCommand { CredentialId = credentialId });
        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["CredentialTestPassed"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadCredentialsAsync();
    }

    private async Task RevokeCredentialAsync(Guid credentialId)
    {
        var parameters = new DialogParameters
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmRevokeCredential"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Revoke"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["RevokeCredential"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled) return;

        var result = await Mediator.Send(new RevokeCredentialCommand { CredentialId = credentialId });
        if (result.IsSuccess)
        {
            Snackbar.Add(Localizer["CredentialRevoked"], Severity.Success);
        }
        else
        {
            Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);
        }

        await LoadCredentialsAsync();
    }

    private async Task ShowConfigureDialogAsync()
    {
        Guid? clinicId = Guid.TryParse(RequestContext.TenantId, out Guid cId) ? cId : null;
        if (clinicId is null) return;

        var parameters = new DialogParameters
        {
            { nameof(CredentialConfigureDialog.ClinicId), clinicId.Value }
        };

        var dialog = await DialogService.ShowAsync<CredentialConfigureDialog>(
            Localizer["ConfigureCredential"], parameters);
        var dialogResult = await dialog.Result;

        if (dialogResult is not null && !dialogResult.Canceled)
        {
            await LoadCredentialsAsync();
        }
    }

    private static Color GetStatusColor(CredentialStatus status) => status switch
    {
        CredentialStatus.Active => Color.Success,
        CredentialStatus.Expired => Color.Error,
        CredentialStatus.Revoked => Color.Dark,
        CredentialStatus.Pending => Color.Warning,
        _ => Color.Default
    };
}
