using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class RemindersGrid : RemindersBase
{
    protected IReadOnlyList<ReminderReadDto>? Reminders;

    protected override async Task ReloadRequired()
    {
        CommunicationsQueryResult<IReadOnlyList<ReminderReadDto>> result = await CommunicationsClient.GetRemindersAsync(new ReminderDescriptorDto(), ComponentCancellationToken);
        Reminders = result.Value ?? [];
        await InvokeAsync(StateHasChanged);
    }

    private SortMethod[] SortMethods => [
        new SortMethod(
            () => Localizer["DateAscending"],
            d =>
            {
                Reminders = Reminders is null ? [] : d ? [.. Reminders.OrderByDescending(r => r.CreatedAt)] : [.. Reminders.OrderBy(r => r.CreatedAt)];
                StateHasChanged();
            })];

    private async Task OnClicked(ReminderReadDto reminder)
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Reminder), reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(null, parameters);
    }
}
