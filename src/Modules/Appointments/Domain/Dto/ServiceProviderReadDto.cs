using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Domain.Dto;

public record ServiceProviderReadDto : EntityReadDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<string> LinkedUserIds { get; set; } = [];
}
