using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class AddressBookDialog : SeaCommonBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public ReminderKind Kind { get; set; }
    [Parameter, EditorRequired] public IEnumerable<ReminderRecipientDto> Recipients { get; set; } = [];

    private HashSet<ReminderRecipientDto> selected = [];
    private string customAddress = string.Empty;

    private IReadOnlyList<UserDto> Users = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadUsersAsync();
        selected = Recipients is not null ? [.. Recipients] : [];
    }
    private async Task LoadUsersAsync()
    {
        IList<User> usersInRole = await UsersService.GetUsersInRoleAsync(RequestContext.GetTenantUserRole(), CancellationToken.None);
        Users = [.. usersInRole.Select(u => new UserDto(u))];
    }

    private void ToggleUser(UserDto user)
    {
        string? address = GetAddress(user, Kind);
        if (string.IsNullOrEmpty(address))
        {
            return;
        }
        ReminderRecipientDto recipient = new(Kind, user.Id, address);
        if (!selected.Add(recipient))
        {
            _ = selected.Remove(recipient);
        }
    }

    private void RemoveRecipient(ReminderRecipientDto recipient) => selected.Remove(recipient);
    private bool IsSelected(UserDto user)
    {
        string? address = GetAddress(user, Kind);
        if (address is null)
        {
            return false;
        }
        ReminderRecipientDto recipient = new(Kind, user.Id, address);
        return selected.Contains(recipient);
    }

    private void AddCustom()
    {
        if (!string.IsNullOrWhiteSpace(customAddress))
        {
            ReminderRecipientDto recipient = new(Kind, null, customAddress);
            _ = selected.Add(recipient);
            customAddress = string.Empty;
        }
    }

    private void Confirm() => MudDialog.Close(DialogResult.Ok(selected.ToList()));

    private string AddressLabel => Kind == ReminderKind.Email ? Localizer["Email"] : Localizer["Phone"];

    private static string? GetAddress(UserDto user, ReminderKind kind)
    => kind switch
    {
        ReminderKind.Email => user.Email,
        ReminderKind.Sms => user.PhoneNumber,
    };

    private void Cancel() => MudDialog.Cancel();
}
