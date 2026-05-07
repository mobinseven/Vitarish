namespace Vitarish.Appointments.Interface.Components.Sessions;

public partial class SessionReminderPolicySummary : SessionsBase
{
    [Parameter, EditorRequired] public Guid SessionId { get; set; }

    private IReadOnlyList<SessionReminderPolicyReadDto> enabledPolicies = [];
    private Guid loadedSessionId;
    private bool isLoading;
    private string? errorMessage;

    private string SummaryHelperText => enabledPolicies.Count == 0
        ? Localizer["AutoRemindersDisabled"].Value
        : Localizer["AutoRemindersEnabled"].Value;

    protected override async Task OnParametersSetAsync()
    {
        if (!AdminMode || SessionId == Guid.Empty || SessionId == loadedSessionId)
        {
            return;
        }

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
                enabledPolicies = [];
                errorMessage = FormatErrors(result.Errors);
                return;
            }

            enabledPolicies = [.. result.Value.Where(static policy => policy.IsEnabled).OrderBy(static policy => policy.OffsetBeforeOccurrenceStart)];
        }
        finally
        {
            isLoading = false;
        }
    }

    private string PolicyLabel(SessionReminderPolicyReadDto policy)
        => $"{Localizer[policy.Kind.ToString()]} · {FormatOffset(policy.OffsetBeforeOccurrenceStart)} {Localizer["Before"]}";

    private string FormatOffset(TimeSpan offset)
    {
        if (offset.TotalDays >= 1 && offset.TotalHours % 24 == 0)
        {
            return $"{offset.TotalDays:0} {Localizer["Days"]}";
        }

        return $"{offset.TotalHours:0.#} {Localizer["Hours"]}";
    }

    private static string FormatErrors(IEnumerable<EntityError> errors)
        => string.Join(Environment.NewLine, errors.Select(static error => error.Message));
}
