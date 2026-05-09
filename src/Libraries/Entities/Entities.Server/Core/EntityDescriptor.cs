namespace Vitarish.Entities.Server.Core;

public record EntityDescriptor
{
    public Guid? Id { get; set; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? Search { get; init; }
    public DateTimeOffset? CreatedFrom { get; init; }
    public DateTimeOffset? CreatedTo { get; init; }
    public DateTimeOffset? DeletedFrom { get; init; }
    public DateTimeOffset? DeletedTo { get; init; }
}
