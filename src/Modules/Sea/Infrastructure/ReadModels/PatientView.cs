namespace Vitarish.Sea.Infrastructure.ReadModels;

internal sealed class PatientView
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? IdentificationCode { get; init; }
    public bool Template { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
}
