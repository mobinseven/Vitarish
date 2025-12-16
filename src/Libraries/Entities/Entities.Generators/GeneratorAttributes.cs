namespace Vitarish.Entities.Generators;

[System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
public sealed class EntityDescriptorForAttribute(System.Type entityType) : System.Attribute
{
    public System.Type EntityType { get; } = entityType;
}

[System.AttributeUsage(System.AttributeTargets.Assembly)]
public sealed class GenerateEntitiesRegistrationAttribute : System.Attribute
{
}

