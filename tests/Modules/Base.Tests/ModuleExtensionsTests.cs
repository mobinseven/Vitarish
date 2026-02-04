using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Vitarish.Modules.Base.Tests;

public class ModuleExtensionsTests
{
    private class TestModuleA : Module
    {
        public override string Name => "TestModuleA";
        public bool Registered { get; private set; }
        public bool EndpointsMapped { get; private set; }

        protected override void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configureDbContext)
        {
            Registered = true;
        }

        protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            EndpointsMapped = true;
        }
    }

    private class TestModuleB : Module
    {
        public override string Name => "TestModuleB";
        public bool Registered { get; private set; }

        protected override void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configureDbContext)
        {
            Registered = true;
        }
    }

    private class DbContextAwareModule : Module
    {
        public override string Name => "DbContextAware";

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
    public void DiscoverModules_FindsModulesInAssembly()
    {
        // Arrange
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;

        // Act
        List<IModule> modules = ModuleExtensions.DiscoverModules([assembly]);

        // Assert
        modules.Should().NotBeEmpty();
        modules.Should().Contain(m => m.Name == "TestModuleA");
        modules.Should().Contain(m => m.Name == "TestModuleB");
    }

    [Fact]
    public void DiscoverModules_DoesNotIncludeAbstractClasses()
    {
        // Arrange
        Assembly assembly = typeof(Module).Assembly;

        // Act
        List<IModule> modules = ModuleExtensions.DiscoverModules([assembly]);

        // Assert
        modules.Should().NotContain(m => m.GetType() == typeof(Module));
    }

    [Fact]
    public void DiscoverModules_DoesNotIncludeInterfaces()
    {
        // Arrange
        Assembly assembly = typeof(IModule).Assembly;

        // Act
        List<IModule> modules = ModuleExtensions.DiscoverModules([assembly]);

        // Assert
        modules.Should().NotContain(m => m.GetType().IsInterface);
    }

    [Fact]
    public async Task AddModules_RegistersAllDiscoveredModules()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;

        // Act
        await builder.AddModules(assembly);

        // Assert
        ServiceProvider sp = builder.Services.BuildServiceProvider();
        IReadOnlyList<IModule>? modules = sp.GetService<IReadOnlyList<IModule>>();

        modules.Should().NotBeNull();
        modules.Should().Contain(m => m.Name == "TestModuleA");
        modules.Should().Contain(m => m.Name == "TestModuleB");
    }

    [Fact]
    public async Task AddModules_CallsRegisterOnAllModules()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;

        // Act
        await builder.AddModules(assembly);

        // Assert
        ServiceProvider sp = builder.Services.BuildServiceProvider();
        IReadOnlyList<IModule>? modules = sp.GetService<IReadOnlyList<IModule>>();

        TestModuleA? testModuleA = modules?.OfType<TestModuleA>().FirstOrDefault();
        TestModuleB? testModuleB = modules?.OfType<TestModuleB>().FirstOrDefault();

        testModuleA?.Registered.Should().BeTrue();
        testModuleB?.Registered.Should().BeTrue();
    }

    [Fact]
    public async Task MapModuleEndpoints_CallsMapEndpointsOnAllModules()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;
        await builder.AddModules(assembly);
        WebApplication app = builder.Build();

        // Act
        await app.InitializeModules();

        // Assert
        IReadOnlyList<IModule>? modules = app.Services.GetService<IReadOnlyList<IModule>>();
        TestModuleA? testModuleA = modules?.OfType<TestModuleA>().FirstOrDefault();

        testModuleA?.EndpointsMapped.Should().BeTrue();
    }

    [Fact]
    public async Task MapModuleEndpoints_WithNoModulesRegistered_DoesNotThrow()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        WebApplication app = builder.Build();

        // Act
        Func<Task> act = async () => await app.InitializeModules();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddModules_WithMultipleAssemblies_RegistersFromAll()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Assembly assembly1 = typeof(ModuleExtensionsTests).Assembly;
        Assembly assembly2 = typeof(IModule).Assembly;

        // Act
        await builder.AddModules(assembly1, assembly2);

        // Assert
        ServiceProvider sp = builder.Services.BuildServiceProvider();
        IReadOnlyList<IModule>? modules = sp.GetService<IReadOnlyList<IModule>>();

        modules.Should().NotBeNull();
        modules.Should().Contain(m => m.Name == "TestModuleA");
    }

    [Fact]
    public async Task AddModules_WithEmptyAssemblyArray_RegistersEmptyList()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddModules([]);

        // Assert
        ServiceProvider sp = builder.Services.BuildServiceProvider();
        IReadOnlyList<IModule>? modules = sp.GetService<IReadOnlyList<IModule>>();

        modules.Should().NotBeNull();
        modules.Should().BeEmpty();
    }

    [Fact]
    public void DiscoverModules_WithLoggerAndValidModules_LogsDiscovery()
    {
        // Arrange
        Mock<ILogger> mockLogger = new();
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;

        // Act
        List<IModule> modules = ModuleExtensions.DiscoverModules([assembly], mockLogger.Object);

        // Assert
        modules.Should().NotBeEmpty();
    }

    [Fact]
    public void DiscoverModules_WithNullLogger_DoesNotThrow()
    {
        // Arrange
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;

        // Act
        Action act = () => ModuleExtensions.DiscoverModules([assembly], null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public async Task AddModules_ForwardsDbContextConfigurator()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        Assembly assembly = typeof(ModuleExtensionsTests).Assembly;
        Action<DbContextOptionsBuilder> configureDbContext = _ => { };

        // Act
        await builder.AddModules(configureDbContext, assembly);

        // Assert
        ServiceProvider sp = builder.Services.BuildServiceProvider();
        DbContextAwareModule? module = sp.GetService<IReadOnlyList<IModule>>()?.OfType<DbContextAwareModule>().FirstOrDefault();
        module.Should().NotBeNull();
        module!.ReceivedConfigurator.Should().BeSameAs(configureDbContext);
    }
}
