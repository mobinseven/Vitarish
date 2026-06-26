
namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftDetail : ShiftsBase
{
    [Parameter, EditorRequired] public ShiftReadDto Shift { get; set; } = default!;
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDeleted { get; set; }
    [Parameter] public EventCallback OnStatusChanged { get; set; }
    [Parameter] public EventCallback<ShiftReservationReadDto> OnShiftOccurrenceSelected { get; set; }

    [Inject] private IServiceProviderManager ServiceProviderManager { get; set; } = default!;
    [Inject] private IServiceManager ServiceManager { get; set; } = default!;

    private string? serviceProviderName;
    private string? serviceName;
    private Guid? lastShiftId;

    private TimeSpan EffectiveEstimatedSessionLength => Shift.EndTime - Shift.StartTime;

    protected override async Task OnParametersSetAsync()
    {
        if (lastShiftId == Shift?.Id)
        {
            return;
        }

        lastShiftId = Shift?.Id;

        Task<ServiceProviderReadDto?> providerTask = Shift?.ServiceProviderId is { } spId
            ? ServiceProviderManager.GetAsync(spId, CancellationToken.None).GetValueOrDefaultAsync()
            : Task.FromResult<ServiceProviderReadDto?>(null);

        Task<ServiceReadDto?> serviceTask = Shift?.ServiceId is { } svcId
            ? ServiceManager.GetAsync(svcId, CancellationToken.None).GetValueOrDefaultAsync()
            : Task.FromResult<ServiceReadDto?>(null);

        await Task.WhenAll(providerTask, serviceTask);

        serviceProviderName = providerTask.Result?.Name;
        serviceName = serviceTask.Result?.Name;
    }

    private async Task ChangeStatusAsync(ShiftStatus status)
    {
        string label = status switch
        {
            ShiftStatus.Active => Localizer["ActivateItem", Localizer["Shift"]].Value,
            ShiftStatus.Paused => Localizer["PauseItem", Localizer["Shift"]].Value,
            ShiftStatus.Closed => Localizer["CloseItem", Localizer["Shift"]].Value,
            ShiftStatus.Cancelled => Localizer["CancelItem", Localizer["Shift"]].Value,
            ShiftStatus.Archived => Localizer["ArchiveItem", Localizer["Shift"]].Value,
            _ => Localizer["Confirm"].Value
        };

        Color color = status is ShiftStatus.Cancelled or ShiftStatus.Archived ? Color.Error : Color.Primary;

        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion"].Value },
            { nameof(ConfirmDialog.ButtonText), label },
            { nameof(ConfirmDialog.Color), color }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(label, parameters);
        DialogResult? result = await dialog.Result;

        if (result is null || result.Canceled)
        {
            return;
        }

        EntityResult entityResult = status switch
        {
            ShiftStatus.Active => await ShiftManager.ActivateShiftAsync(Shift.Id, CancellationToken.None),
            ShiftStatus.Paused => await ShiftManager.PauseShiftAsync(Shift.Id, CancellationToken.None),
            ShiftStatus.Closed => await ShiftManager.CloseShiftAsync(Shift.Id, CancellationToken.None),
            ShiftStatus.Cancelled => await ShiftManager.CancelShiftAsync(Shift.Id, CancellationToken.None),
            ShiftStatus.Archived => await ShiftManager.ArchiveShiftAsync(Shift.Id, CancellationToken.None),
            _ => EntityResult.Success()
        };

        if (entityResult.IsSuccess)
        {
            ShiftReadDto? updated = await ShiftManager.GetAsync(Shift.Id, CancellationToken.None).GetValueOrDefaultAsync();
            if (updated is not null)
            {
                Shift = updated;
            }

            await OnStatusChanged.InvokeAsync();
            await InvokeAsync(StateHasChanged);
        }
    }
}
