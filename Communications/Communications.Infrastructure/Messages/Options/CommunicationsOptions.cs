namespace Vitarish.Communications.Infrastructure.Messages.Options;

public record CommunicationsOptions
{
    public const string Section = "Communications";

    public SmsDotIrOptions SmsDotIr { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
    
    /// <summary>
    /// Authority URL for the Identity service (e.g., "https://identity.example.com")
    /// Used for health monitoring.
    /// </summary>
    public string? IdentityAuthority { get; set; }
    
    /// <summary>
    /// Phone number to receive health check failure notifications
    /// </summary>
    public string? HealthCheckNotificationRecipient { get; set; }
}
