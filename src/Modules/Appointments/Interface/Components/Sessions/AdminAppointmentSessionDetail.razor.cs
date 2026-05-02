namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class AdminAppointmentSessionDetail : AppointmentSessionsBase
{
    [Parameter, EditorRequired] public AppointmentSessionReadDto Session { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }

    [Inject] protected IAccessControlledAppointmentsService AppointmentsService { get; set; } = default!;

    private AppointmentSessionReadDto current = default!;
    private List<AppointmentReadDto> linkedAppointments = [];

    protected override void OnParametersSet()
    {
        current = Session;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadLinkedAppointmentsAsync();
    }

    private async Task LoadLinkedAppointmentsAsync()
    {
        AppointmentDescriptorDto descriptor = new()
        {
            SessionId = current.Id,
            Skip = 0,
            Take = int.MaxValue
        };

        IReadOnlyList<AppointmentReadDto>? appointments = await AppointmentsService.GetListAsync(descriptor, CancellationToken.None).GetValueOrDefaultAsync();
        linkedAppointments = [.. (appointments ?? [])];
        await InvokeAsync(StateHasChanged);
    }

    private async Task ChangeStatusAsync(AppointmentSessionStatus status)
    {
        string label = status switch
        {
            AppointmentSessionStatus.Cancelled => Localizer["CancelItem", Localizer["AppointmentSession"]].Value,
            AppointmentSessionStatus.Completed => Localizer["CompleteItem", Localizer["AppointmentSession"]].Value,
            _ => Localizer["Confirm"].Value
        };

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), label },
            { nameof(ConfirmDialog.Color), status == AppointmentSessionStatus.Cancelled ? Color.Error : Color.Dark }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(label, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        AppointmentSessionWriteDto writeDto = current.ToWrite();
        writeDto.Status = status;
        await SessionsService.UpdateAsync(writeDto, CancellationToken.None);

        AppointmentSessionReadDto? updated = await SessionsService.GetAsync(current.Id, CancellationToken.None).GetValueOrDefaultAsync();
        if (updated is not null)
        {
            current = updated;
        }

        await OnStatusChanged.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task DeleteSessionAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["DeleteItemConfirm", Localizer["AppointmentSession"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["Delete"].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["AppointmentSession"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        await SessionsService.RemoveAsync(current.Id, CancellationToken.None);
        await OnDeleted.InvokeAsync();
    }
}
