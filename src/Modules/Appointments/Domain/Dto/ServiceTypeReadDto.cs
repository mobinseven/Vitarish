using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record ServiceTypeReadDto : EntityReadDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
