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

    private void ToggleUser(object user)
    {
        string? id = GetStringProperty(user, "Id");
        if (string.IsNullOrWhiteSpace(id)) return;
        string? address = GetUserAddressById(id, Kind);
        if (string.IsNullOrEmpty(address)) return;
        ReminderRecipientReadDto? recipient = selected.SingleOrDefault(r => r.RecipientUserId == id);
        _ = recipient is null ? selected.Add(new() { Kind = Kind, RecipientUserId = id, Recipient = address }) : selected.Remove(recipient);
        StateHasChanged();
    }

    private void RemoveRecipient(ReminderRecipientReadDto recipient) => selected.Remove(recipient);

    private bool IsSelected(object user)
    {
        string? id = GetStringProperty(user, "Id");
        return id is not null && GetUserAddressById(id, Kind) is not null && selected.Any(r => r.RecipientUserId == id);
    }

    private static string? GetStringProperty(object value, string propertyName) => value.GetType().GetProperty(propertyName)?.GetValue(value)?.ToString();

    private static string GetUserLabel(object user) => GetStringProperty(user, "FullName") ?? GetStringProperty(user, "UserName") ?? GetStringProperty(user, "Id") ?? string.Empty;

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
