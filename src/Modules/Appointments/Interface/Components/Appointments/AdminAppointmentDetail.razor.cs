namespace Vitarish.Appointments.Interface.Components.Appointments;

public partial class AdminAppointmentDetail : AppointmentsBase
{
    [Parameter, EditorRequired] public AppointmentReadDto Appointment { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    private AppointmentReadDto current = default!;

    protected override void OnParametersSet()
    {
        current = Appointment;
    }

    private async Task ChangeStatusAsync(AppointmentStatus status)
    {
        string label = status switch
        {
            AppointmentStatus.Confirmed => Localizer["ConfirmAppointment"].Value,
            AppointmentStatus.Completed => Localizer["CompleteAppointment"].Value,
            AppointmentStatus.Cancelled => Localizer["CancelAppointment"].Value,
            _ => Localizer["Confirm"].Value
        };

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), label },
            { nameof(ConfirmDialog.Color), status == AppointmentStatus.Cancelled ? Color.Error : Color.Primary }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(label, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await AppointmentsService.UpdateStatusAsync(current.Id, status, CancellationToken.None);

        AppointmentReadDto? updated = await AppointmentsService.GetAsync(current.Id, CancellationToken.None).GetValueOrDefaultAsync();
        if (updated is not null)
        {
            current = updated;
        }

        await OnStatusChanged.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteAppointmentAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["Appointment"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Appointment"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await AppointmentsService.RemoveAsync(current.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
