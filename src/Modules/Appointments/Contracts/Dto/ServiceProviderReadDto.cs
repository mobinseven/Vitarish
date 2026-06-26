namespace Vitarish.Appointments.Contracts.Dto;

public record ServiceProviderReadDto : EntityReadDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<string> LinkedUserIds { get; set; } = [];
}
