namespace Vitarish.Appointments.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<SessionReminderKind>))]
public enum SessionReminderKind
{
    Email,
    Sms
}
