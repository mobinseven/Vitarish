using System.Reflection;
using Vitarish.Modules.Base;

namespace Modules.Base.Tests;

public class IModuleInterfaceTests
{
    [Fact]
    public void IModule_HasNameProperty()
    {
        // Arrange & Act
        PropertyInfo? property = typeof(IModule).GetProperty("Name");

        // Assert
        _ = property.Should().NotBeNull();
        _ = property!.PropertyType.Should().Be(typeof(string));
        _ = property.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IModule_HasRegisterMethod()
    {
        // Arrange & Act
        MethodInfo? method = typeof(IModule).GetMethod("Register");

        // Assert
        _ = method.Should().NotBeNull();
        _ = method!.ReturnType.Should().Be(typeof(Task));
        _ = method.GetParameters().Should().HaveCount(2);
        _ = method.GetParameters()[0].ParameterType.Should().Be(typeof(WebApplicationBuilder));
        _ = method.GetParameters()[1].ParameterType.Should().Be(typeof(Action<DbContextOptionsBuilder>));
    }

    [Fact]
    public void IModule_HasMapEndpointsMethod()
    {
        // Arrange & Act
        MethodInfo? method = typeof(IModule).GetMethod("Initialize");

        // Assert
        _ = method.Should().NotBeNull();
        _ = method!.ReturnType.Should().Be(typeof(Task));
        _ = method.GetParameters().Should().HaveCount(1);
        _ = method.GetParameters()[0].ParameterType.Should().Be(typeof(IEndpointRouteBuilder));
    }
}
