namespace Vitarish.Communications.Infrastructure.Messages.Options;

public record CommunicationsOptions
{
    public const string Section = "Communications";

    public SmsDotIrOptions SmsDotIr { get; set; } = new();
    public SmtpOptions Smtp { get; set; } = new();
}
