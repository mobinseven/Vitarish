namespace Vitarish.Sea.Domain.Dto;

public sealed record ReminderRecipientDto(
    [property: JsonPropertyName("kind")] ReminderKind Kind,
    [property: JsonPropertyName("recipientUserId")] string? RecipientUserId,
    [property: JsonPropertyName("recipient")] string Recipient);
