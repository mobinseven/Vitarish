namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class UserReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;

    private ReminderKind kind = ReminderKind.Sms;
    private DateTime? remindDate = DateTime.Now.Date;
    private TimeSpan? remindTime = DateTime.Now.TimeOfDay;

    private MudStepper? MudStepper;
    private bool showKindStep;
    private bool KindStep;
    private bool DateStep;
    private bool TimeStep;
    private bool ContentStep;
    private DateTimeOffset remindAt => new(remindDate!.Value.Date + remindTime!.Value, TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow));
    private string subject = string.Empty;
    private string textBody = string.Empty;

    private int ActiveStep
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;
            ResetCompletedSteps(value);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        subject = Localizer["ReminderSubject", Event.Title];
        textBody = await GetTextBody();

        string? phone = HttpContextAccessor.HttpContext?.GetUserPhoneNumber();
        string? email = HttpContextAccessor.HttpContext?.GetUserEmail();

        showKindStep = !string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(email);
        if (!showKindStep)
        {
            kind = !string.IsNullOrWhiteSpace(phone) ? ReminderKind.Sms : ReminderKind.Email;
            KindStep = true;
        }
    }

    private async Task SaveAsync()
    {
        List<ReminderRecipientDto> recipients = [new ReminderRecipientDto(kind, RequestContext.UserId, GetAddress(kind))];
        IReadOnlyList<ReminderDto> added = await RemindersService.AddRangeAsync(Event.Id, recipients, subject, textBody, remindAt, CancellationToken.None);
        MudDialog.Close(DialogResult.Ok(added));
    }

    private void Cancel() => MudDialog.Cancel();

    private async Task MoveToPreviousStepAsync()
    {
        if (MudStepper is null)
        {
            return;
        }

        await MudStepper.PreviousStepAsync();
    }

    private async Task<string> GetTextBody()
    {
        UsageConfigurationDto? usageConfig = await UsageConfigurationService.GetUsageConfigurationAsync(CancellationToken.None);
        string firstLine = Localizer["ReminderText", Event.Title, Procedure.Name, Project.Name];
        return string.Concat(firstLine, Environment.NewLine, Environment.NewLine, Event.Description, Environment.NewLine, Environment.NewLine, usageConfig?.ReminderSignature);
    }

    private string? GetAddress(ReminderKind kind)
        => kind switch
        {
            ReminderKind.Email => HttpContextAccessor.HttpContext?.GetUserEmail(),
            ReminderKind.Sms => HttpContextAccessor.HttpContext?.GetUserPhoneNumber(),
        };

    private void ResetCompletedSteps(int currentStep)
    {
        if (showKindStep)
        {
            if (currentStep <= 3)
            {
                ContentStep = false;
            }

            if (currentStep <= 2)
            {
                TimeStep = false;
            }

            if (currentStep <= 1)
            {
                DateStep = false;
            }

            if (currentStep <= 0)
            {
                KindStep = false;
            }
        }
        else
        {
            if (currentStep <= 2)
            {
                ContentStep = false;
            }

            if (currentStep <= 1)
            {
                TimeStep = false;
            }

            if (currentStep <= 0)
            {
                DateStep = false;
            }
        }
    }
}
