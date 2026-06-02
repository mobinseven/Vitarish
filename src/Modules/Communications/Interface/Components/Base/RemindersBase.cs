using Vitarish.Modules.Base;

namespace Vitarish.Communications.Interface.Components.Base;

public class RemindersBase : TenantCommonBase
{
    [Inject] protected IRemindersService RemindersService { get; set; } = default!;

    protected string? GetUserAddressById(string userId, ReminderKind kind)
        => kind switch
        {
            ReminderKind.Email => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.Email,
            ReminderKind.Sms => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.PhoneNumber,
        };

    protected string? GetUserAddressById(Guid userId, ReminderKind kind) => GetUserAddressById(userId.ToString(), kind);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReminderReadDto>();
    }
}
