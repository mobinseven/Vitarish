using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Appointments.Contracts.Dto;

public record ServiceProviderWriteDto : EntityWriteDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IList<string> LinkedUserIds { get; set; } = [];
}
