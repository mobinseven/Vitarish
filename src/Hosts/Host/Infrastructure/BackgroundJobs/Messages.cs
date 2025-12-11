namespace Vitarish.Host.Infrastructure.BackgroundJobs;

/// <summary>
/// Base class for all messages processed by background jobs.
/// </summary>
public abstract record Message;

/// <summary>
/// Represents an email message to be sent.
/// </summary>
public record EmailMessage(string To, string Subject, string TextBody, string HtmlBody) : Message;

/// <summary>
/// Represents an SMS message to be sent.
/// </summary>
public record SmsMessage(string PhoneNumber, string Code) : Message;
