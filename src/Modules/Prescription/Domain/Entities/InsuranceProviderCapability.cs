using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Records which capabilities a specific provider supports.
/// </summary>
public sealed class InsuranceProviderCapability
{
    public Guid Id { get; set; }
    public Guid InsuranceProviderId { get; set; }
    public InsuranceProviderCapabilityType Capability { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Configuration { get; set; }
}
