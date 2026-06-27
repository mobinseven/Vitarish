using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Communications.Interface.Components.Base;

public class RemindersBase : TenantCommonBase, IDisposable
{
    private readonly CancellationTokenSource disposalCancellation = new();

    [Inject] protected ICommunicationsClient CommunicationsClient { get; set; } = default!;

    protected string? GetUserAddressById(string userId, ReminderKind kind)
        => kind switch
        {
            ReminderKind.Email => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.Email,
            ReminderKind.Sms => TenantService.Users?.FirstOrDefault(x => x.Id == userId)?.PhoneNumber,
            _ => null
        };

    protected string? GetUserAddressById(Guid userId, ReminderKind kind) => GetUserAddressById(userId.ToString(), kind);

    protected CancellationToken ComponentCancellationToken => disposalCancellation.Token;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        RenderForType<ReminderReadDto>();
    }

    public void Dispose()
    {
        disposalCancellation.Cancel();
        disposalCancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
