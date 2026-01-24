using Vitarish.Sea.Interface.Services;

namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : CommonBase, IDisposable
{
    [Inject] protected IMetadataService MetadataService { get; set; } = default!;
    /// <summary>
    /// Optional override for admin-specific rendering when the caller wants to force admin mode.
    /// </summary>
    [CascadingParameter] public bool IsAdmin { get; set; } = false;
    /// <summary>
    /// Effective admin flag resolved from the override or the access service.
    /// </summary>
    protected bool AdminMode => IsAdmin;
    protected IEnumerable<UserDto> GetUsers() => MetadataService.Users ?? [];
    protected string? GetUserFullNameById(Guid id) => MetadataService.Users?.FirstOrDefault(x => x.Id == id.ToString())?.FullName ?? id.ToString();
    protected string? GetUserFullNameById(string id) => MetadataService.Users?.FirstOrDefault(x => x.Id == id)?.FullName ?? id;

    protected string? GetUserAddressById(string userId, ReminderKind kind)
    => kind switch
    {
        ReminderKind.Email => MetadataService.Users?.FirstOrDefault(x => x.Id == userId)?.Email,
        ReminderKind.Sms => MetadataService.Users?.FirstOrDefault(x => x.Id == userId)?.PhoneNumber,
    };
    protected string? GetUserAddressById(Guid userId, ReminderKind kind) => GetUserAddressById(userId.ToString(), kind);
}
