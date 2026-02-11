namespace Vitarish.Sea.Domain.Dto;

public sealed class ReminderRecipientReadDto
{
    [JsonPropertyName("kind")]
    public ReminderKind Kind { get; set; }

    [JsonPropertyName("recipientUserId")]
    public string? RecipientUserId { get; set; }

    [JsonPropertyName("recipient")]
    public string Recipient { get; set; } = default!;
}
