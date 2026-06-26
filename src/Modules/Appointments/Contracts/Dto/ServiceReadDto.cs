namespace Vitarish.Appointments.Contracts.Dto;

public record ServiceReadDto : EntityReadDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ServiceTypeId { get; set; }
    public string? ServiceTypeName { get; set; }
}
