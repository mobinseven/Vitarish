namespace Vitarish.Communications.Contracts.Responses;
public sealed record SmsSendResponse(long? MessageId, string? Status, string? MessageText);
public sealed record SmsDeliveryStateResponse(long MessageId, string? Status, string? MessageText, DateTimeOffset? CheckedAt);
