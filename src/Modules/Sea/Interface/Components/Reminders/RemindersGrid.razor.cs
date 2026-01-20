using Vitarish.Interfaces.Base.Models;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class RemindersGrid : RemindersBase
{
    protected IReadOnlyList<ReminderDto>? Reminders;

    protected override async Task ReloadRequired()
    {
        Reminders = await RemindersService.GetListAsync(new ReminderDescriptorDto(), CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        await InvokeAsync(StateHasChanged);
    }

    private SortMethod[] SortMethods => [
        new SortMethod(
            () => Localizer["DateAscending"],
            () =>
            {
                Reminders = [.. Reminders.OrderBy(r => r.CreatedAt)];
                StateHasChanged();
            }),

        new SortMethod(
            () => Localizer["DateDescending"],
            () =>
            {
                Reminders = [.. Reminders.OrderByDescending(r => r.CreatedAt)];
                StateHasChanged();
            })];

    private async Task OnClicked(ReminderDto reminder)
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Reminder), reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(null, parameters);
    }
}
