using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Vitarish.Main.Infrastructure.Options;

public sealed class AuditCapacityAlertOptions
{
    public const string SectionName = "Vitarish:Main:AuditCapacityAlert";
    public const string EmailRecipientKey = SectionName + ":EmailRecipient";
    public const string SmsRecipientKey = SectionName + ":SmsRecipient";

    public static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(30);

    public string EmailRecipient { get; init; } = string.Empty;
    public string SmsRecipient { get; init; } = string.Empty;

    public bool HasRecipients =>
        !string.IsNullOrWhiteSpace(EmailRecipient) &&
        !string.IsNullOrWhiteSpace(SmsRecipient);
}

internal sealed class AuditCapacityAlertOptionsValidator(
    IHostEnvironment environment,
    bool openApiGeneration) : IValidateOptions<AuditCapacityAlertOptions>
{
    private const string UnassignedNorthAmericanAreaCode = "555";

    private static readonly string[] ReservedEmailDomains =
    [
        "example.com",
        "example.edu",
        "example.net",
        "example.org",
        "invalid",
        "localhost",
        "test"
    ];

    public ValidateOptionsResult Validate(string? name, AuditCapacityAlertOptions options)
    {
        if (openApiGeneration || !environment.IsProduction())
            return ValidateOptionsResult.Success;

        List<string> failures = [];

        if (!options.HasRecipients)
        {
            failures.Add($"{AuditCapacityAlertOptions.EmailRecipientKey} and {AuditCapacityAlertOptions.SmsRecipientKey} must be configured");
        }
        else
        {
            if (!IsRoutableEmail(options.EmailRecipient))
                failures.Add($"{AuditCapacityAlertOptions.EmailRecipientKey} must be a non-placeholder email address");
            if (!IsE164Phone(options.SmsRecipient))
                failures.Add($"{AuditCapacityAlertOptions.SmsRecipientKey} must be a non-placeholder E.164 phone number");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(string.Join("; ", failures));
    }

    private static bool IsRoutableEmail(string value)
    {
        if (!string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            !MailAddress.TryCreate(value, out MailAddress? address) ||
            !string.Equals(address.Address, value, StringComparison.Ordinal) ||
            Uri.CheckHostName(address.Host) != UriHostNameType.Dns ||
            !address.Host.Contains('.'))
            return false;

        return !ReservedEmailDomains.Any(domain =>
            string.Equals(address.Host, domain, StringComparison.OrdinalIgnoreCase) ||
            address.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsE164Phone(string value)
    {
        if (!Regex.IsMatch(value, @"^\+[1-9][0-9]{7,14}$", RegexOptions.CultureInvariant))
            return false;

        // 555 is an unassigned North American area code used by the deployment example.
        return !(value.StartsWith("+1", StringComparison.Ordinal) &&
            value.Length >= 5 &&
            value.AsSpan(2, 3).SequenceEqual(UnassignedNorthAmericanAreaCode));
    }
}
