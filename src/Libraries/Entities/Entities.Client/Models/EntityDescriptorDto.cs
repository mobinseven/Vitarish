namespace Vitarish.Entities.Client.Models;

public record EntityDescriptorDto
{
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public bool? IsDeleted { get; set; }
    public string? Search { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedFrom { get; set; }
    public DateTimeOffset? UpdatedTo { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset? DeletedFrom { get; set; }
    public DateTimeOffset? DeletedTo { get; set; }
    public string? DeletedBy { get; set; }
}

