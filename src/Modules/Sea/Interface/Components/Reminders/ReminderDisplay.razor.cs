using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderDisplay : SeaCommonBase
{
    [Parameter] public Color Color { get; set; }
    [Parameter, EditorRequired] public ReminderDto Reminder { get; set; } = default!;
    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;


    private IReadOnlyList<UserDto> Users = [];

    protected override async Task OnInitializedAsync() => await LoadUsersAsync();
    private async Task LoadUsersAsync()
    {
        IList<User> usersInRole = await UsersService.GetUsersInRoleAsync("User", CancellationToken.None);
        Users = [.. usersInRole.Select(u => new UserDto(u))];
    }

    private async Task ShowDetails()
    {
        DialogParameters parameters = new()
        {
            { nameof(ReminderDetailsDialog.Event), Event },
            { nameof(ReminderDetailsDialog.Project), Project },
            { nameof(ReminderDetailsDialog.Procedure), Procedure },
            { nameof(ReminderDetailsDialog.Reminder), Reminder }
        };

        _ = await DialogService.ShowAsync<ReminderDetailsDialog>(Localizer["Reminder"], parameters);
    }
    private string? UserIdToName(string? id) => id is null ? null : Users.FirstOrDefault(x => x.Id == id)?.FullName;

}

