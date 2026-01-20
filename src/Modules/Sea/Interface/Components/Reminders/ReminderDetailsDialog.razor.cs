using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderDetailsDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReminderDto Reminder { get; set; } = default!;

    private bool CanEdit => AdminMode || RequestContext.UserId == Reminder.CreatedBy;

    private IReadOnlyList<UserDto> Users = [];

    protected override async Task ReloadRequired()
    {
        IList<User> usersInRole = await UsersService.GetUsersInRoleAsync(RequestContext.GetTenantUserRole(), CancellationToken.None);
        Users = [.. usersInRole.Select(u => new UserDto(u))];
    }

    private string? UserIdToName(string? id) => id is null ? null : Users.FirstOrDefault(x => x.Id == id)?.FullName;

    private string SentAtText() => Reminder.SentAt is null
        ? Localizer["NotSent"].Value
        : $"{Reminder.SentAt.Value.ToLocalTime().DateTime:d} {Reminder.SentAt.Value.ToLocalTime().DateTime:t}";

    private async Task DeleteAsync()
    {
        DialogParameters parameters = new()
        {
            { nameof(ConfirmDialog.ContentText), Localizer["ConfirmationQuestion", Localizer["Reminder"]].Value },
            { nameof(ConfirmDialog.ButtonText), Localizer["DeleteItem", Localizer["Reminder"]].Value },
            { nameof(ConfirmDialog.Color), Color.Error }
        };
        IDialogReference dialog = await DialogService.ShowAsync<ConfirmDialog>(Localizer["DeleteItem", Localizer["Reminder"]], parameters);
        DialogResult? result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }
        _ = await RemindersService.RemoveAsync(Reminder.Id, CancellationToken.None);
        MudDialog.Close();
    }

    private void Close() => MudDialog.Close();
}
