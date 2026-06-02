namespace Vitarish.Communications.Domain.Models;

public class Message
{
    /// <summary>
    /// The sender's address
    /// </summary>
    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    /// <summary>
    /// The recipient's address
    /// </summary>
    [JsonPropertyName("recipient")]
    public string? Recipient { get; set; }

    /// <summary>
    /// An optional subject line
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    /// <summary>
    /// The message HTML body
    /// </summary>
    [JsonPropertyName("htmlBody")]
    public string? HtmlBody { get; set; }

    /// <summary>
    /// The message text body
    /// </summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
