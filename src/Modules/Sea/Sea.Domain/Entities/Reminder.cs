namespace Vitarish.Sea.Domain.Entities;

public class Reminder : EventItem
{
    private string _recipient = string.Empty;
    private string _recipientUserId = string.Empty;
    private string _subject = string.Empty;
    private string _textBody = string.Empty;
    private DateTimeOffset _remindAt;
    private DateTimeOffset? _sentAt;
    private ReminderKind _kind;

    public ReminderKind Kind
    {
        get => _kind;
        private set => SetField(ref _kind, value, nameof(Kind));
    }

    public string Recipient
    {
        get => _recipient;
        set => SetField(ref _recipient, value, nameof(Recipient));
    }

    public string RecipientUserId
    {
        get => _recipientUserId;
        set => SetField(ref _recipientUserId, value, nameof(RecipientUserId));
    }

    public string Subject
    {
        get => _subject;
        set => SetField(ref _subject, value, nameof(Subject));
    }

    public string TextBody
    {
        get => _textBody;
        set => SetField(ref _textBody, value, nameof(TextBody));
    }

    public DateTimeOffset RemindAt
    {
        get => _remindAt;
        set => SetField(ref _remindAt, value, nameof(RemindAt));
    }

    public DateTimeOffset? SentAt
    {
        get => _sentAt;
        private set => SetField(ref _sentAt, value, nameof(SentAt));
    }

    public bool IsSent => _sentAt != null;

    public Reminder(Guid eventId, ReminderKind kind, string recipientUserId, string recipient, string subject, string textBody, DateTimeOffset remindAt)
        : base(eventId)
    {
        _kind = kind;
        _recipientUserId = recipientUserId;
        _recipient = recipient;
        _subject = subject;
        _textBody = textBody;
        _remindAt = remindAt;
    }

    public void MarkSent(DateTimeOffset sentAt)
    {
        SentAt = sentAt;
    }

    public void SetKind(ReminderKind kind)
    {
        Kind = kind;
    }
}

