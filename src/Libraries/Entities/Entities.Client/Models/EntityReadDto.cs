namespace Vitarish.Entities.Client.Models;

public abstract record EntityReadDto : EntityWriteDto
{
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public bool IsDeleted => DeletedAt is not null;
}
