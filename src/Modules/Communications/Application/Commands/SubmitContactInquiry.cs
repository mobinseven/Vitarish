using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Vitarish.Communications.Application.Commands;

public sealed record ContactInquiry(
    string Name,
    string Email,
    string Goal,
    string Need,
    string Message);

public static class SubmitContactInquiry
{
    public static async Task ExecuteAsync(
        IMessagesService service,
        string recipient,
        ContactInquiry inquiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(inquiry);

        string normalizedRecipient = Require(recipient, nameof(recipient), 254);
        string name = Require(inquiry.Name, nameof(inquiry.Name), 120);
        string email = Require(inquiry.Email, nameof(inquiry.Email), 254);
        string goal = Require(inquiry.Goal, nameof(inquiry.Goal), 80);
        string need = Require(inquiry.Need, nameof(inquiry.Need), 80);
        string message = Require(inquiry.Message, nameof(inquiry.Message), 2000);

        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new ArgumentException("Email must be a valid address.", nameof(inquiry));
        }

        cancellationToken.ThrowIfCancellationRequested();

        string textBody = $"""
            New Vitarish contact inquiry

            Name: {name}
            Email: {email}
            Goal: {goal}
            Need: {need}

            Message:
            {message}
            """;

        string htmlBody = $"""
            <h1>New Vitarish contact inquiry</h1>
            <p><strong>Name:</strong> {WebUtility.HtmlEncode(name)}</p>
            <p><strong>Email:</strong> {WebUtility.HtmlEncode(email)}</p>
            <p><strong>Goal:</strong> {WebUtility.HtmlEncode(goal)}</p>
            <p><strong>Need:</strong> {WebUtility.HtmlEncode(need)}</p>
            <p><strong>Message:</strong></p>
            <p>{WebUtility.HtmlEncode(message).Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "<br>", StringComparison.Ordinal)}</p>
            """;

        await service.SendEmail(
            new Message
            {
                Recipient = normalizedRecipient,
                Subject = $"Vitarish contact: {goal} / {need}",
                Body = textBody,
                HtmlBody = htmlBody
            },
            cancellationToken);
    }

    private static string Require(string? value, string name, int maxLength)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            throw new ArgumentException($"{name} is required.", name);
        }

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{name} must be {maxLength} characters or fewer.", name);
        }

        return normalized;
    }
}
