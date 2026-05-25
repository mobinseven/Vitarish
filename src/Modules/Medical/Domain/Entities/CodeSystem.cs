namespace Vitarish.Medical.Domain.Entities;

public sealed class CodeSystem : Entity
{
    public required string SystemKey { get; set; }

    public required string Name { get; set; }

    public string? Authority { get; set; }

    public CodeSystemCategory Category { get; set; }

    public string? CanonicalUri { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
