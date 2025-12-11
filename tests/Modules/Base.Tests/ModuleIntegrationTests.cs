namespace Vitarish.Modules.Base.Tests;

/// <summary>
/// Integration tests that validate the module system works correctly
/// in a simulated web application environment.
/// </summary>
public class ModuleIntegrationTests
{
    private class ServiceRegistrationModule : ModuleBase
    {
        public override string Name => "ServiceRegistration";

        public static bool ServiceRegistered { get; private set; }
        public static bool EndpointsMapped { get; private set; }

        public static void Reset()
        {
            ServiceRegistered = false;
            EndpointsMapped = false;
        }

        protected override void ConfigureServices(
            IServiceCollection services,
            IConfiguration configuration,
            Action<DbContextOptionsBuilder>? configureDbContext)
        {
            services.AddSingleton<TestService>();
            ServiceRegistered = true;
        }

        protected override void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
        {
            EndpointsMapped = true;
        }
    }

    private class TestService
    {
        public string GetValue() => "test-value";
    }

    [Fact]
    public async Task FullModuleLifecycle_RegistersServicesAndMapsEndpoints()
    {
        // Arrange
        ServiceRegistrationModule.Reset();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();

        // Act - Register modules
        await builder.AddModules(typeof(ModuleIntegrationTests).Assembly);
        WebApplication app = builder.Build();

        // Act - Map endpoints
        await app.MapModuleEndpoints();

        // Assert
        ServiceRegistrationModule.ServiceRegistered.Should().BeTrue();
        ServiceRegistrationModule.EndpointsMapped.Should().BeTrue();

        // Verify the service was actually registered
        TestService? service = app.Services.GetService<TestService>();
        service.Should().NotBeNull();
        service!.GetValue().Should().Be("test-value");
    }

    [Fact]
    public async Task ModulesAreAvailableViaServiceProvider()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        // Act
        await builder.AddModules(typeof(ModuleIntegrationTests).Assembly);
        WebApplication app = builder.Build();

        // Assert
        IReadOnlyList<IModule>? modules = app.Services.GetService<IReadOnlyList<IModule>>();
        modules.Should().NotBeNull();
        modules!.Should().NotBeEmpty();
        modules.Should().Contain(m => m.Name == "ServiceRegistration");
    }

    [Fact]
    public async Task EmptyAssemblyList_DoesNotFail()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddLogging();

        // Act
        await builder.AddModules([]);
        WebApplication app = builder.Build();
        await app.MapModuleEndpoints();

        // Assert - should not throw
        IReadOnlyList<IModule>? modules = app.Services.GetService<IReadOnlyList<IModule>>();
        modules.Should().NotBeNull();
        modules!.Should().BeEmpty();
    }
}
