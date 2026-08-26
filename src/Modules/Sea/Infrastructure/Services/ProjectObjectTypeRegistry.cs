namespace Vitarish.Sea.Infrastructure.Services;

internal sealed class ProjectObjectTypeRegistry(IEnumerable<ProjectObjectTypeDefinition> definitions) : IProjectObjectTypeRegistry
{
    private readonly IReadOnlySet<(string OwnerEntityType, string ObjectType)> _definitions = definitions
        .Select(definition => (definition.OwnerEntityType, definition.ObjectType))
        .ToHashSet();

    public bool IsRegistered(string ownerEntityType, string objectType)
        => _definitions.Contains((ownerEntityType, objectType));
}
