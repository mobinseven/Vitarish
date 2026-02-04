using System;
using System.Reflection;

namespace Vitarish.Modules.Base.Tests;

public class ModuleBaseTests
{
    private class TestModule : Module
    {
        public override string Name => "TestModule";

        public bool ConfigureServicesCalled { get; private set; }
        public bool ConfigureEndpointsCalled { get; private set; }
        public bool InitializeAsyncCalled { get; private set; }

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

        protected override Task InitializeAsync(WebApplication app)
        {
            InitializeAsyncCalled = true;
            return Task.CompletedTask;
        }
    }

    private class MinimalModule : Module
    {
        public override string Name => "MinimalModule";
    }

    private class DbContextConfiguredModule : Module
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
        module.InitializeAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task MapEndpoints_CallsAllEndpointConfigureMethods()
    {
        // Arrange
        TestModule module = new();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        // Act
        await module.Initialize(app);

        // Assert
        module.ConfigureEndpointsCalled.Should().BeTrue();
        module.InitializeAsyncCalled.Should().BeTrue();
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
        Func<Task> act = async () => await module.Initialize(app);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void ModuleBase_ImplementsIModule()
    {
        // Assert
        typeof(Module).Should().Implement<IModule>();
    }
}
