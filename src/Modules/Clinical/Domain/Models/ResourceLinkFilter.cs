namespace Vitarish.Clinical.Domain.Models;

public sealed record ResourceLinkFilter
{
    public string? LocalEntityType { get; init; }
    public Guid? LocalEntityId { get; init; }
    public string? ResourceType { get; init; }
    public string? ResourceId { get; init; }
    public bool IncludeArchived { get; init; }
    public string? SearchText { get; init; }
    public int Take { get; init; } = 50;
}
