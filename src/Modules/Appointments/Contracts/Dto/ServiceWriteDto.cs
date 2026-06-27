using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Contracts.Dto;

public record ServiceWriteDto : EntityWriteDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ServiceTypeId { get; set; }
}
