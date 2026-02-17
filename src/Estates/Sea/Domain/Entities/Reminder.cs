namespace Vitarish.Sea.Domain.Entities;

public class Reminder : EventItem
{
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

    public DateTimeOffset RemindAt
    {
        get;
        set => SetField(ref field, value, nameof(RemindAt));
    }

    public DateTimeOffset? SentAt
    {
        get;
        private set => SetField(ref field, value, nameof(SentAt));
    }

    public bool IsSent => SentAt != null;

    public void MarkSent(DateTimeOffset sentAt)
    {
        SentAt = sentAt;
    }

    public void SetKind(ReminderKind kind)
    {
        Kind = kind;
    }
}

