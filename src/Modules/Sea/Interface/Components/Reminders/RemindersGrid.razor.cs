namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class RemindersGrid : RemindersBase
{
    protected IReadOnlyList<ReminderDto>? Reminders;

    private enum SortBy
    {
        At, AtDescending
    }

    private string? sortMethod;

    protected override async Task ReloadRequired()
    {
        Reminders = await RemindersService.GetListAsync(new ReminderDescriptorDto(), CancellationToken.None).GetValueOrDefaultAsync() ?? [];
        Sort();
        await InvokeAsync(StateHasChanged);
    }

    private void Sort(string? sort = default)
    {
        sortMethod = sort;
        Reminders = sortMethod == SortBy.At.ToString()
            ? [.. Reminders.OrderBy(r => r.CreatedAt)]
            : [.. Reminders.OrderByDescending(r => r.CreatedAt)];
    }

    private async Task OnClicked(ReminderDto reminder)
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Reminder), reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(null, parameters);
    }
}
