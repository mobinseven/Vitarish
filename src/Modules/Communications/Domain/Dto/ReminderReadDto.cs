using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Communications.Domain.Dto;

public record ReminderReadDto : EntityReadDto
{
    [JsonPropertyName("sourceId")]
    public Guid SourceId { get; set; }

    [JsonPropertyName("sourceKind")]
    public string SourceKind { get; set; } = "manual";

    [JsonPropertyName("templatePurpose")]
    public string TemplatePurpose { get; set; } = string.Empty;

    [JsonPropertyName("templateVariablesJson")]
    public string? TemplateVariablesJson { get; set; }

    [JsonPropertyName("kind")]
    public ReminderKind Kind { get; set; }

    [JsonPropertyName("recipientUserId")]
    public string? RecipientUserId { get; set; }

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = default!;

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = default!;

    [JsonPropertyName("textBody")]
    public string TextBody { get; set; } = default!;

    [JsonPropertyName("remindAt")]
    public DateTime RemindAt { get; set; }

    [JsonPropertyName("sentAt")]
    public DateTime? SentAt { get; set; }

    [JsonPropertyName("isSent")]
    public bool IsSent { get; set; }

    [JsonPropertyName("providerMessageId")]
    public string? ProviderMessageId { get; set; }

    [JsonPropertyName("providerSubmittedAt")]
    public DateTime? ProviderSubmittedAt { get; set; }

    [JsonPropertyName("deliveryAcceptedAt")]
    public DateTime? DeliveryAcceptedAt { get; set; }

    [JsonPropertyName("url")]
    public string Url => $"/reminders/{Id}";
}
