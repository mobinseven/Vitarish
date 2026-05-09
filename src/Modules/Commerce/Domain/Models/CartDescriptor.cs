namespace Vitarish.Commerce.Domain.Models;

[EntityDescriptorFor(typeof(Cart))]
public record CartDescriptor : EntityDescriptor
{
    public string? CustomerId { get; init; }
    public CartStatus? Status { get; init; }
    public CartRecoveryStatus? RecoveryStatus { get; init; }
}
