namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPoliciesList : SessionReminderPoliciesBase
{
    [Parameter, EditorRequired] public Guid SessionId { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private List<SessionReminderPolicyReadDto> policies = [];

    protected override async Task OnParametersSetAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        IReadOnlyList<SessionReminderPolicyReadDto> result = await ReminderPolicyManager.GetBySessionAsync(SessionId, CancellationToken.None);
        policies = [.. result];
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(SessionReminderPolicyDialog.SessionId), SessionId }
        };

        IDialogReference dialog = await DialogService.ShowAsync<SessionReminderPolicyDialog>(
            Localizer["AddItem", Localizer["Policy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task EditAsync(SessionReminderPolicyReadDto policy)
    {
        DialogParameters parameters = new()
        {
            { nameof(SessionReminderPolicyDialog.SessionId), SessionId },
            { nameof(SessionReminderPolicyDialog.Policy), policy }
        };

        IDialogReference dialog = await DialogService.ShowAsync<SessionReminderPolicyDialog>(
            Localizer["EditItem", Localizer["Policy"]], parameters);
        DialogResult? result = await dialog.Result;

        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }

    private async Task RemoveAsync(SessionReminderPolicyReadDto policy)
    {
        EntityResult result = await ReminderPolicyManager.RemovePolicyAsync(SessionId, policy.Id, CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            await OnChanged.InvokeAsync();
        }
    }
}
