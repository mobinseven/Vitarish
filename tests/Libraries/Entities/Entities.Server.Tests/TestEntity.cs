[assembly: GenerateEntitiesRegistration]

namespace Entities.Server.Tests;

[EntityDescriptorFor(typeof(TestEntity))]
public record TestDescriptor : EntityDescriptor;

public class TestEntity : Entity
{
    public TestEntity()
    {
        Id = Guid.NewGuid();
    }

    public string Name { get; set; } = string.Empty;
}
