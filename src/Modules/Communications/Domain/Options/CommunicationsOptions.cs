namespace Vitarish.Communications.Domain.Options;

public record SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool EnableSsl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public record ProvidersOptions
{
    public SmsDotIrOptions SmsDotIr { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
}

public record NotificationOptions
{
    public string StartupSmsRecipient { get; set; } = string.Empty;
    public string HealthFailureSmsRecipient { get; set; } = string.Empty;
    public string ContactEmailRecipient { get; set; } = string.Empty;
}
