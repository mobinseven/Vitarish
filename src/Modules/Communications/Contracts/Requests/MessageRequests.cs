namespace Vitarish.Communications.Contracts.Requests;
public sealed record SendEmailRequest(string Recipient, string Subject, string TextBody, string HtmlBody);
public sealed record SendSmsRequest(string Recipient, string Body);
public sealed record SendSmsCodeRequest(string Recipient, string Code);
public sealed record SendSmsReminderRequest(string Recipient, string? Project = null, string? Procedure = null, string? Event = null, string? Tenant = null);
