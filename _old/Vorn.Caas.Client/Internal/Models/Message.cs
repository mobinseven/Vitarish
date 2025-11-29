using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Vorn.Caas.Client.Internal.Models;

internal class Message
{
    [JsonPropertyName("sender")]
    public string? Sender { get; set; }

    [Required]
    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = default!;

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("htmlBody")]
    public string? HtmlBody { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }
}
