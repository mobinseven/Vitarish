namespace Vitarish.Modules.Base.Tests;

public class IModuleInterfaceTests
{
    [Fact]
    public void IModule_HasNameProperty()
    {
        // Arrange & Act
        var property = typeof(IModule).GetProperty("Name");

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(string));
        property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IModule_HasRegisterMethod()
    {
        // Arrange & Act
        var method = typeof(IModule).GetMethod("Register");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
        method.GetParameters().Should().HaveCount(2);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(WebApplicationBuilder));
        method.GetParameters()[1].ParameterType.Should().Be(typeof(Action<DbContextOptionsBuilder>));
    }

    [Fact]
    public void IModule_HasMapEndpointsMethod()
    {
        // Arrange & Act
        var method = typeof(IModule).GetMethod("MapEndpoints");

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task));
        method.GetParameters().Should().HaveCount(1);
        method.GetParameters()[0].ParameterType.Should().Be(typeof(IEndpointRouteBuilder));
    }
}
