namespace Vitarish.Entities.Client.Models;

public record EntityDescriptorDto
{
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public bool? IsDeleted { get; set; }
    public string? Search { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedFrom { get; set; }
    public DateTime? UpdatedTo { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedFrom { get; set; }
    public DateTime? DeletedTo { get; set; }
    public string? DeletedBy { get; set; }
}

