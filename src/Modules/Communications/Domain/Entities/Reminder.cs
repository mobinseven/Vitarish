namespace Vitarish.Communications.Domain.Entities;

public class Reminder : Entity
{
    public Guid SourceId
    {
        get;
        set => SetField(ref field, value, nameof(SourceId));
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

    public string RecipientUserId
    {
        get;
        set => SetField(ref field, value, nameof(RecipientUserId));
    } = string.Empty;

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

    public void MarkSent(DateTime sentAt) => SentAt = sentAt;

    public void SetKind(ReminderKind kind) => Kind = kind;
}
