namespace Vitarish.Communications.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ReminderKind>))]
public enum ReminderKind
{
    Email,
    Sms
}
