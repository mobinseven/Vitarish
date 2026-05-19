namespace Vitarish.Communications.Application.Abstractions;

public abstract record CommunicationCommand;

public sealed record EmailCommunicationCommand(
    string Recipient,
    string Subject,
    string TextBody,
    string HtmlBody) : CommunicationCommand;

public sealed record SmsCommunicationCommand(
    string Recipient,
    string Body) : CommunicationCommand;

public sealed record SmsCodeCommunicationCommand(
    string Recipient,
    string Code) : CommunicationCommand;

public sealed record SmsReminderCommunicationCommand(
    string Recipient,
    string? Project = null,
    string? Procedure = null,
    string? Event = null,
    string? Tenant = null) : CommunicationCommand;
