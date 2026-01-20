namespace Vitarish.Sea.Interface.Components.Reminders;

public partial class ReminderCreatorDialog : RemindersBase
{
    [CascadingParameter] private IMudDialogInstance MudDialog { get; set; } = default!;

    [Parameter, EditorRequired] public EventDto Event { get; set; } = default!;
    [Parameter, EditorRequired] public ProjectDto Project { get; set; } = default!;
    [Parameter, EditorRequired] public ProcedureDto Procedure { get; set; } = default!;

    private ReminderKind kind = ReminderKind.Sms;
    private List<ReminderRecipientDto> recipients = [];
    private DateTime? remindDate = DateTime.Now.Date;
    private TimeSpan? remindTime = DateTime.Now.TimeOfDay;

    private MudStepper? MudStepper;
    private bool RecipientsStep;
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
        await base.OnInitializedAsync();
        subject = Localizer["ReminderSubject", Event.Title];
        textBody = await GetTextBody();
    }

    private async Task SaveAsync()
    {
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
        string eventUri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri).ToString();
        return string.Concat(firstLine, Environment.NewLine, Environment.NewLine, eventUri, Environment.NewLine, Environment.NewLine, usageConfig?.ReminderSignature);
    }

    private async Task AddRecipientAsync()
    {
        DialogParameters parameters = new() { [nameof(AddressBookDialog.Kind)] = kind, [nameof(AddressBookDialog.Recipients)] = recipients };
        IDialogReference dialog = await DialogService.ShowAsync<AddressBookDialog>(Localizer["Receivers"], parameters);
        DialogResult? result = await dialog.Result;

        if (result is null)
        {
            return;
        }

        if (!result.Canceled && result.Data is IEnumerable<ReminderRecipientDto> rec)
        {
            recipients = [.. rec];
        }
    }

    private void ResetCompletedSteps(int currentStep)
    {
        if (currentStep <= 4)
        {
            RecipientsStep = false;
        }

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
}
