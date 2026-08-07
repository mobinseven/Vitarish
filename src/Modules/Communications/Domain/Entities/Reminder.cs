namespace Vitarish.Communications.Domain.Entities;

public class Reminder : Entity
{
    public Guid SourceId
    {
        get;
        set => SetField(ref field, value, nameof(SourceId));
    }

    public string SourceKind
    {
        get;
        set => SetField(ref field, value, nameof(SourceKind));
    } = "manual";

    public string TemplatePurpose
    {
        get;
        set => SetField(ref field, value, nameof(TemplatePurpose));
    } = string.Empty;

    public string? TemplateVariablesJson
    {
        get;
        set => SetField(ref field, value, nameof(TemplateVariablesJson));
    }

    public ReminderKind Kind
    {
        get;
        set => SetField(ref field, value, nameof(Kind));
    }

    public string Recipient
    {
        get;
        set => SetField(ref field, value, nameof(Recipient));
    } = string.Empty;

    public string? RecipientUserId
    {
        get;
        set => SetField(ref field, value, nameof(RecipientUserId));
    }

    public string Subject
    {
        get;
        set => SetField(ref field, value, nameof(Subject));
    } = string.Empty;

    public string TextBody
    {
        get;
        set => SetField(ref field, value, nameof(TextBody));
    } = string.Empty;

    public DateTime RemindAt
    {
        get;
        set => SetField(ref field, value, nameof(RemindAt));
    }

    public DateTime? SentAt
    {
        get;
        private set => SetField(ref field, value, nameof(SentAt));
    }

    public bool IsSent => SentAt != null;

    public string? ProviderMessageId
    {
        get;
        private set => SetField(ref field, value, nameof(ProviderMessageId));
    }

    public DateTime? ProviderSubmittedAt
    {
        get;
        private set => SetField(ref field, value, nameof(ProviderSubmittedAt));
    }

    public DateTime? DeliveryAcceptedAt
    {
        get;
        private set => SetField(ref field, value, nameof(DeliveryAcceptedAt));
    }

    public void MarkSent(DateTime sentAt) => SentAt = sentAt;

    public void RecordProviderSubmission(string providerMessageId, DateTime submittedAt)
    {
        ProviderMessageId = providerMessageId;
        ProviderSubmittedAt = submittedAt;
        DeliveryAcceptedAt = submittedAt;
    }

    public void RecordDeliveryAcceptance(DateTime acceptedAt) => DeliveryAcceptedAt = acceptedAt;

    public void SetKind(ReminderKind kind) => Kind = kind;
}
