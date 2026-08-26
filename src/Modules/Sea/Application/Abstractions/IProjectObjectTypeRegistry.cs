namespace Vitarish.Sea.Application.Abstractions;

public sealed record ProjectObjectTypeDefinition(string OwnerEntityType, string ObjectType);

public interface IProjectObjectTypeRegistry
{
    bool IsRegistered(string ownerEntityType, string objectType);
}
