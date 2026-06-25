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

    private void ToggleUser(AddressBookUser user)
    {
        string? id = user.Id;
        if (string.IsNullOrWhiteSpace(id)) return;
        string? address = GetUserAddressById(id, Kind);
        if (string.IsNullOrEmpty(address)) return;
        ReminderRecipientReadDto? recipient = selected.SingleOrDefault(r => r.RecipientUserId == id);
        if (recipient is null)
        {
            selected.Add(new() { Kind = Kind, RecipientUserId = id, Recipient = address });
        }
        else
        {
            selected.Remove(recipient);
        }
        StateHasChanged();
    }

    private void RemoveRecipient(ReminderRecipientReadDto recipient) => selected.Remove(recipient);

    private bool IsSelected(AddressBookUser user)
    {
        string? id = user.Id;
        return id is not null && GetUserAddressById(id, Kind) is not null && selected.Any(r => r.RecipientUserId == id);
    }

    private IEnumerable<AddressBookUser> GetAddressBookUsers() => GetUsers()
        .Select(user => new AddressBookUser(user.Id, user.UserName, user.FullName))
        .Where(user => !string.IsNullOrEmpty(GetUserAddressById(user.Id, Kind)));

    private static string GetUserLabel(AddressBookUser user) => !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.UserName ?? user.Id;

    private sealed record AddressBookUser(string Id, string? UserName, string FullName);

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
