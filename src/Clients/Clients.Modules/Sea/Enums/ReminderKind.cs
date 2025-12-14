namespace Vitarish.Clients.Modules.Sea.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ReminderKind>))]
public enum ReminderKind
{
    Email,
    Sms
}

