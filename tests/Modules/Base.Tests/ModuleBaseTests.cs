using System;
using System.Reflection;

namespace Vitarish.Modules.Base.Tests;

public class ModuleBaseTests
{
    private class TestModule : ModuleBase
    {
        public override string Name => "TestModule";

        public bool ConfigureServicesCalled { get; private set; }
        public bool ConfigureEndpointsCalled { get; private set; }
        public bool ConfigureEndpointsAsyncCalled { get; private set; }
        public bool ConfigureHealthChecksCalled { get; private set; }
        public bool ConfigureSelfTestsCalled { get; private set; }

        protected override void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configureDbContext)
        {
            ConfigureServicesCalled = true;
        }

        protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            ConfigureEndpointsCalled = true;
        }

        protected override Task ConfigureEndpointsAsync(IEndpointRouteBuilder endpoints)
        {
            ConfigureEndpointsAsyncCalled = true;
            return Task.CompletedTask;
        }

        protected override void ConfigureHealthChecks(WebApplicationBuilder builder)
        {
            ConfigureHealthChecksCalled = true;
        }

        protected override void ConfigureSelfTests(WebApplicationBuilder builder)
        {
            ConfigureSelfTestsCalled = true;
        }
    }

    private class MinimalModule : ModuleBase
    {
        public override string Name => "MinimalModule";
    }

    private class DbContextConfiguredModule : ModuleBase
    {
        public override string Name => "DbContextConfigured";

        public Action<DbContextOptionsBuilder>? ReceivedConfigurator { get; private set; }

        protected override void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configureDbContext)
        {
            ReceivedConfigurator = configureDbContext;
        }
    }

    [Fact]
    public void Name_ReturnsModuleName()
    {
        // Arrange
        TestModule module = new();

        // Act
        string name = module.Name;

        // Assert
        name.Should().Be("TestModule");
    }

    [Fact]
    public async Task Register_CallsAllConfigureMethods()
    {
        // Arrange
        TestModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        await module.Register(builder);

        // Assert
        module.ConfigureServicesCalled.Should().BeTrue();
        module.ConfigureHealthChecksCalled.Should().BeTrue();
        module.ConfigureSelfTestsCalled.Should().BeTrue();
    }

    [Fact]
    public async Task MapEndpoints_CallsAllEndpointConfigureMethods()
    {
        // Arrange
        TestModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act
        await module.MapEndpoints(app);

        // Assert
        module.ConfigureEndpointsCalled.Should().BeTrue();
        module.ConfigureEndpointsAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithMinimalModule_DoesNotThrow()
    {
        // Arrange
        MinimalModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        Func<Task> act = async () => await module.Register(builder);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Register_ForwardsDbContextConfigurator()
    {
        // Arrange
        DbContextConfiguredModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Action<DbContextOptionsBuilder> configureDbContext = _ => { };

        // Act
        await module.Register(builder, configureDbContext);

        // Assert
        module.ReceivedConfigurator.Should().BeSameAs(configureDbContext);
    }

    [Fact]
    public async Task MapEndpoints_WithMinimalModule_DoesNotThrow()
    {
        // Arrange
        MinimalModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act
        Func<Task> act = async () => await module.MapEndpoints(app);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ModuleBase_ImplementsIModule()
    {
        // Assert
        typeof(ModuleBase).Should().Implement<IModule>();
    }
}
