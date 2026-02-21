using Vitarish.Modules.Base;

namespace Vitarish.Sea.Interface.Components.Base;

public class SeaCommonBase : TenantCommonBase, IDisposable
{
    protected string? GetUserAddressById(string userId, ReminderKind kind)
    => kind switch
    {
        ReminderKind.Email => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.Email,
        ReminderKind.Sms => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.PhoneNumber,
    };
    protected string? GetUserAddressById(Guid userId, ReminderKind kind) => GetUserAddressById(userId.ToString(), kind);
}
