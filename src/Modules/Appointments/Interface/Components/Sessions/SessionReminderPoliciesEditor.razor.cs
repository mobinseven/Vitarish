namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPoliciesEditor : SessionsBase
{
    [Parameter] public Guid SessionId { get; set; }
    [Parameter] public EventCallback<IReadOnlyList<SessionReminderPolicyWriteDto>> PoliciesChanged { get; set; }

    private readonly List<SessionReminderPolicyWriteDto> policies = [];
    private Guid loadedSessionId;
    private bool autoRemindersEnabled;
    private bool isLoading;
    private bool isSaving;
    private string? errorMessage;

    protected override async Task OnParametersSetAsync()
    {
        if (!AdminMode || SessionId == Guid.Empty || SessionId == loadedSessionId)
        {
            return;
        }

        await LoadPoliciesAsync();
    }

    private async Task LoadPoliciesAsync()
    {
        loadedSessionId = SessionId;
        isLoading = true;
        errorMessage = null;
        try
        {
            EntityResult<IReadOnlyList<SessionReminderPolicyReadDto>> result = await AppointmentReminderService.GetSessionReminderPoliciesAsync(
                SessionId,
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                policies.Clear();
                autoRemindersEnabled = false;
                errorMessage = FormatErrors(result.Errors);
                await NotifyPoliciesChangedAsync();
                return;
            }

            policies.Clear();
            policies.AddRange(result.Value.Select(static policy => policy.ToWrite()));
            autoRemindersEnabled = policies.Any(static policy => policy.IsEnabled);
            await NotifyPoliciesChangedAsync();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SetAutoRemindersEnabledAsync(bool enabled)
    {
        autoRemindersEnabled = enabled;
        if (enabled && policies.Count == 0)
        {
            policies.Add(CreateDefaultPolicy());
        }

        foreach (SessionReminderPolicyWriteDto policy in policies)
        {
            policy.IsEnabled = enabled;
        }

        await NotifyPoliciesChangedAsync();
    }

    private async Task AddPolicyAsync()
    {
        policies.Add(CreateDefaultPolicy());
        autoRemindersEnabled = true;
        await NotifyPoliciesChangedAsync();
    }

    private async Task RemovePolicyAsync(SessionReminderPolicyWriteDto policy)
    {
        _ = policies.Remove(policy);
        autoRemindersEnabled = policies.Any(static p => p.IsEnabled);
        await NotifyPoliciesChangedAsync();
    }

    private async Task NotifyPoliciesChangedAsync()
    {
        autoRemindersEnabled = policies.Any(static policy => policy.IsEnabled);
        errorMessage = ValidatePolicies();
        IReadOnlyList<SessionReminderPolicyWriteDto> snapshot = policies
            .Select(policy => policy with { SessionId = SessionId })
            .ToList();
        await PoliciesChanged.InvokeAsync(snapshot);
    }

    private static SessionReminderPolicyWriteDto CreateDefaultPolicy() => new()
    {
        IsEnabled = true,
        Kind = SessionReminderKind.Email,
        OffsetBeforeOccurrenceStart = TimeSpan.FromHours(24),
        QualifyingReservationStatuses = [ReservationStatus.Confirmed]
    };

    private string? ValidatePolicies()
    {
        if (!policies.All(static policy => Enum.IsDefined(policy.Kind)))
        {
            return Localizer["ReminderPolicyChannelUnsupported"].Value;
        }

        if (policies.Any(static policy => policy.OffsetBeforeOccurrenceStart <= TimeSpan.Zero))
        {
            return Localizer["ReminderPolicyOffsetMustBePositive"].Value;
        }

        return null;
    }

    private static string FormatErrors(IEnumerable<EntityError> errors)
        => string.Join(Environment.NewLine, errors.Select(static error => error.Message));
}
