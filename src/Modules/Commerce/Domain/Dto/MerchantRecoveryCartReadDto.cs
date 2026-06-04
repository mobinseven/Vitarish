namespace Vitarish.Commerce.Domain.Dto;

public record MerchantRecoveryCartReadDto
{
    public Guid CartId { get; init; }
    public string CustomerDisplay { get; init; } = "Registered customer";
    public string CustomerId { get; init; } = string.Empty;
    public bool HasContact { get; init; }
    public decimal EstimatedValue { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public CartRecoveryStatus RecoveryStatus { get; init; }
    public string RecoveryNote { get; init; } = string.Empty;
}
