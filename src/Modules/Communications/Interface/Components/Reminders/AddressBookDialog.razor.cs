using Vitarish.Users.Domain.Models;

namespace Vitarish.Communications.Interface.Components.Reminders;

public partial class AddressBookDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReminderKind Kind { get; set; }
    [Parameter, EditorRequired] public IEnumerable<ReminderRecipientReadDto> Recipients { get; set; } = [];

    private HashSet<ReminderRecipientReadDto> selected = [];
    private string customAddress = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        selected = Recipients is not null ? [.. Recipients] : [];
    }

    private void ToggleUser(UserDto user)
    {
        string? address = GetUserAddressById(user.Id, Kind);
        if (string.IsNullOrEmpty(address))
        {
            return;
        }
        ReminderRecipientReadDto? recipient = selected.SingleOrDefault(r => r.RecipientUserId == user.Id);
        _ = recipient is null
            ? selected.Add(new() { Kind = Kind, RecipientUserId = user.Id, Recipient = address })
            : selected.Remove(recipient);
        StateHasChanged();
    }

    private void RemoveRecipient(ReminderRecipientReadDto recipient) => selected.Remove(recipient);
    private bool IsSelected(UserDto user)
    {
        string? address = GetUserAddressById(user.Id, Kind);
        return address is not null && selected.Any(r => r.RecipientUserId == user.Id);
    }

    private void AddCustom()
    {
        if (!string.IsNullOrWhiteSpace(customAddress))
        {
            ReminderRecipientReadDto recipient = new() { Kind = Kind, RecipientUserId = null, Recipient = customAddress };
            _ = selected.Add(recipient);
            customAddress = string.Empty;
        }
    }

    private void Confirm() => MudDialog.Close(DialogResult.Ok(selected.ToList()));

    private string AddressLabel => Kind == ReminderKind.Email ? Localizer["Email"] : Localizer["Phone"];

    private void Cancel() => MudDialog.Cancel();
}
