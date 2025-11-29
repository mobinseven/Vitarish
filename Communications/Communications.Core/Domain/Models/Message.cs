namespace Vitarish.Communications.Core.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a message to be sent via email or SMS.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the sender's address.
    /// </summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    /// <summary>
    /// Gets or sets the recipient's address.
    /// </summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    /// <summary>
    /// Gets or sets an optional subject line.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the message HTML body.
    /// </summary>
    [JsonPropertyName("htmlBody")]
    public string? HtmlBody { get; set; }

    /// <summary>
    /// Gets or sets the message text body.
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
