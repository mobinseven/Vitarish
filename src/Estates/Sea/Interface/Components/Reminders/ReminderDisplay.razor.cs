using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderDisplay : SeaCommonBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public ReminderReadDto Reminder { get; set; } = default!;
    private async Task ShowDetails()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Reminder), Reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(Localizer["Reminder"], parameters);
    }
}

