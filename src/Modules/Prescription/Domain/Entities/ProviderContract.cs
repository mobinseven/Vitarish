namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Represents a clinic's contract with an insurance provider.
/// </summary>
public sealed class ProviderContract
{
    public Guid Id { get; set; }
    public Guid InsuranceProviderId { get; set; }
    public Guid ClinicId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
