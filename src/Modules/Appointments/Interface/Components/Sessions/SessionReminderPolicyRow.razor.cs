namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPolicyRow : SessionsBase
{
    [Parameter, EditorRequired] public SessionReminderPolicyWriteDto Policy { get; set; } = default!;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public EventCallback<SessionReminderPolicyWriteDto> OnRemove { get; set; }
    [Parameter] public EventCallback OnChanged { get; set; }

    private static IReadOnlyList<SessionReminderKind> SupportedChannels { get; } = Enum.GetValues<SessionReminderKind>();

    private int OffsetHours => Math.Max(1, (int)Math.Ceiling(Policy.OffsetBeforeOccurrenceStart.TotalHours));

    private async Task SetEnabledAsync(bool enabled)
    {
        Policy.IsEnabled = enabled;
        await OnChanged.InvokeAsync();
    }

    private async Task SetKindAsync(SessionReminderKind kind)
    {
        Policy.Kind = kind;
        await OnChanged.InvokeAsync();
    }

    private async Task SetOffsetHours(int value)
    {
        Policy.OffsetBeforeOccurrenceStart = TimeSpan.FromHours(Math.Max(1, value));
        await OnChanged.InvokeAsync();
    }

    private async Task RemoveAsync() => await OnRemove.InvokeAsync(Policy);
}
