namespace Vitarish.Commerce.Domain.Dto;

public record CategoryWriteDto : EntityWriteDto
{
    public string? MedusaId { get; set; }
    public string? Name { get; set; }
    public string? Handle { get; set; }
    public Guid? ParentCategoryId { get; set; }
}
