namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftBookingPoliciesList : ShiftBookingPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private List<ShiftBookingPolicyReadDto> policies = [];

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IReadOnlyList<ShiftBookingPolicyReadDto> result = await BookingPolicyManager.GetByShiftAsync(ShiftId, CancellationToken.None);
        policies = [.. result];
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ShiftBookingPolicyDialog.ShiftId), ShiftId }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ShiftBookingPolicyDialog>(
            Localizer["AddItem", Localizer["BookingPolicy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task EditAsync(ShiftBookingPolicyReadDto policy)
    {
        DialogParameters parameters = new()
        {
            { nameof(ShiftBookingPolicyDialog.ShiftId), ShiftId },
            { nameof(ShiftBookingPolicyDialog.Policy), policy }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ShiftBookingPolicyDialog>(
            Localizer["EditItem", Localizer["BookingPolicy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task RemoveAsync(ShiftBookingPolicyReadDto policy)
    {
        EntityResult result = await BookingPolicyManager.RemovePolicyAsync(ShiftId, policy.Id, CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }
}
