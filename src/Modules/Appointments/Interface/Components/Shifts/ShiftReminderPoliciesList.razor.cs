namespace Vitarish.Appointments.Interface.Components.Shifts;

public partial class ShiftReminderPoliciesList : ShiftReminderPoliciesBase
{
    [Parameter, EditorRequired] public Guid ShiftId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private List<ShiftReminderPolicyReadDto> policies = [];

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        EntityResult<IReadOnlyList<ShiftReminderPolicyReadDto>> result = await ReminderPolicyManager.GetListAsync(
            new ShiftReminderPolicyDescriptorDto { ShiftId = ShiftId }, CancellationToken.None);
        policies = [.. result.Value ?? []];
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ShiftReminderPolicyDialog.ShiftId), ShiftId }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ShiftReminderPolicyDialog>(
            Localizer["AddItem", Localizer["Policy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task EditAsync(ShiftReminderPolicyReadDto policy)
    {
        DialogParameters parameters = new()
        {
            { nameof(ShiftReminderPolicyDialog.ShiftId), ShiftId },
            { nameof(ShiftReminderPolicyDialog.Policy), policy }
        };

        IDialogReference dialog = await DialogService.ShowAsync<ShiftReminderPolicyDialog>(
            Localizer["EditItem", Localizer["Policy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task RemoveAsync(ShiftReminderPolicyReadDto policy)
    {
        EntityResult result = await ReminderPolicyManager.RemoveAsync(policy.Id, CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }
}
