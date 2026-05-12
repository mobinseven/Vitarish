namespace Vitarish.Sea.Domain.Models;

[EntityDescriptorFor(typeof(Project))]
public record ProjectDescriptor : EntityDescriptor
{
    public bool? Template { get; init; } = false;
    public string? IdentificationCode { get; init; }
    public ProjectSortBy? SortBy { get; init; }
    public bool Descending { get; init; }
    public IReadOnlyList<Guid>? RoleIds { get; init; }
    public IReadOnlyList<string>? UserIds { get; init; }
    public bool IncludeStats { get; init; } = true;
}

