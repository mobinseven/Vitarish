using Vitarish.Prescription.Domain.Enums;

namespace Vitarish.Prescription.Domain.Entities;

/// <summary>
/// Registry entry for a supported insurance provider.
/// </summary>
public sealed class InsuranceProvider
{
    public Guid Id { get; set; }
    public required string ProviderKey { get; set; }
    public required string Name { get; set; }
    public string? ProviderType { get; set; }
    public IntegrationMode IntegrationMode { get; set; }
    public ProviderStatus Status { get; set; } = ProviderStatus.Disabled;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
