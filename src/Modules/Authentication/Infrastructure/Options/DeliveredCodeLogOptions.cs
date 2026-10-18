namespace Vitarish.Authentication.Infrastructure.Options;

/// <summary>
/// Temporary, owner-approved exception to the rule that sign-in codes are never
/// logged (issue #893): when enabled, the email and SMS code senders log each
/// delivered code and its purpose, never the recipient, so a sample deployment
/// can be signed into without mailbox access. Off by default. Remove this option
/// and its log calls when issue #893 closes.
/// </summary>
public sealed class DeliveredCodeLogOptions
{
    public const string SectionName = "Vitarish:Security:DeliveredCodeLog";

    public bool Enabled { get; set; }
}
