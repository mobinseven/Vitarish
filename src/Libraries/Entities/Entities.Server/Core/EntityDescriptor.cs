namespace Vitarish.Entities.Server.Core;

public record EntityDescriptor
{
    public Guid? Id { get; set; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
    public string? Search { get; init; }
    public DateTime? CreatedFrom { get; init; }
    public DateTime? CreatedTo { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime? UpdatedFrom { get; init; }
    public DateTime? UpdatedTo { get; init; }
    public string? UpdatedBy { get; init; }
    public DateTime? DeletedFrom { get; init; }
    public DateTime? DeletedTo { get; init; }
    public string? DeletedBy { get; init; }
}
