namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftBookingPoliciesList : ShiftBookingPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private List<ShiftBookingPolicyReadDto> policies = [];

    protected override async Task OnParametersSetAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        EntityResult<IReadOnlyList<ShiftBookingPolicyReadDto>> result = await BookingPolicyManager.GetListAsync(
            new ShiftBookingPolicyDescriptorDto { ShiftId = ShiftId }, CancellationToken.None);
        policies = [.. result.Value ?? []];
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync() { /* unchanged dialog flow */
        DialogParameters parameters = new() { { nameof(ShiftBookingPolicyDialog.ShiftId), ShiftId } };
        IDialogReference dialog = await DialogService.ShowAsync<ShiftBookingPolicyDialog>(Localizer["AddItem", Localizer["BookingPolicy"]], parameters);
        DialogResult? result = await dialog.Result;
        if (result is not null && !result.Canceled) { await LoadAsync(); await OnChanged.InvokeAsync(); }
    }

    private async Task EditAsync(ShiftBookingPolicyReadDto policy)
    {
        DialogParameters parameters = new() { { nameof(ShiftBookingPolicyDialog.ShiftId), ShiftId }, { nameof(ShiftBookingPolicyDialog.Policy), policy } };
        IDialogReference dialog = await DialogService.ShowAsync<ShiftBookingPolicyDialog>(Localizer["EditItem", Localizer["BookingPolicy"]], parameters);
        DialogResult? result = await dialog.Result;
        if (result is not null && !result.Canceled) { await LoadAsync(); await OnChanged.InvokeAsync(); }
    }

    private async Task RemoveAsync(ShiftBookingPolicyReadDto policy)
    {
        EntityResult result = await BookingPolicyManager.RemoveAsync(policy.Id, CancellationToken.None);
        if (result.IsSuccess) { await LoadAsync(); await OnChanged.InvokeAsync(); }
    }
}
