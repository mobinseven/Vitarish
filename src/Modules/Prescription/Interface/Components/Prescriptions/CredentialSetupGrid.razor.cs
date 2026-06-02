namespace Vitarish.Prescription.Interface.Components.Prescriptions;

public partial class CredentialSetupGrid : PrescriptionBase
{
    private IReadOnlyList<CredentialSetupDto>? credentials;

    protected override async Task OnInitializedAsync() => await LoadCredentialsAsync();

    private async Task LoadCredentialsAsync()
    {
        Guid? clinicId = Guid.TryParse(RequestContext.TenantId, out Guid cId) ? cId : null;
        if (clinicId is null)
        {
            return;
        }

        credentials = await Mediator.Send(new GetCredentialSetupGridQuery
        {
            ClinicId = clinicId.Value
        });
        await InvokeAsync(StateHasChanged);
    }

    private async Task TestCredentialAsync(Guid credentialId)
    {
        TestCredentialResult result = await Mediator.Send(new TestCredentialCommand { CredentialId = credentialId });
        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["CredentialTestPassed"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadCredentialsAsync();
    }

    private async Task RevokeCredentialAsync(Guid credentialId)
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmRevokeCredential"].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Revoke"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["RevokeCredential"], parameters);
        DialogResult? dialogResult = await dialog.Result;

        if (dialogResult is null || dialogResult.Canceled)
        {
            return;
        }

        RevokeCredentialResult result = await Mediator.Send(new RevokeCredentialCommand { CredentialId = credentialId });
        _ = result.IsSuccess
            ? Snackbar.Add(Localizer["CredentialRevoked"], Severity.Success)
            : Snackbar.Add(result.ErrorMessage ?? Localizer["UnknownError"], Severity.Error);

        await LoadCredentialsAsync();
    }

    private async Task ShowConfigureDialogAsync()
    {
        Guid? clinicId = Guid.TryParse(RequestContext.TenantId, out Guid cId) ? cId : null;
        if (clinicId is null)
        {
            return;
        }

        DialogParameters parameters = new()
        {
            { nameof(CredentialConfigureDialog.ClinicId), clinicId.Value }
        };

        IDialogReference dialog = await DialogService.ShowAsync<CredentialConfigureDialog>(
            Localizer["ConfigureCredential"], parameters);
        DialogResult? dialogResult = await dialog.Result;

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
