using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Modules.Base;

namespace Hosts.Main.Tests;

public class ApplicationOptionsExtensionsTests
{
    [Fact]
    public void AddApplicationOptions_DelegatesModuleOptionBindingThroughApplicationModulePath()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vitarish:Example:Enabled"] = "true",
                ["Vitarish:Ai:Enabled"] = "true",
                ["Vitarish:ElasticSearch:Url"] = "http://legacy:9200"
            })
            .Build();
        CapturingModule module = new("Example");

        _ = services.AddApplicationOptions(configuration, [module]);

        module.CapturedSectionPath.Should().Be($"{ApplicationOptions.Section}:Example");
    }

    [Fact]
    public void AddApplicationOptions_DoesNotRequireLegacyModulePaths()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vitarish:Intelligence:DefaultResponseModel"] = "gpt-test",
                ["Vitarish:Search:Url"] = "http://search:9200"
            })
            .Build();
        CapturingModule intelligence = new("Intelligence");
        CapturingModule search = new("Search");

        _ = services.AddApplicationOptions(configuration, [intelligence, search]);

        intelligence.CapturedSectionPath.Should().Be("Vitarish:Intelligence");
        search.CapturedSectionPath.Should().Be("Vitarish:Search");
    }

    private sealed class CapturingModule(string name) : IModule
    {
        public string Name { get; } = name;
        public bool IsAvailable { get; set; }
        public string? CapturedSectionPath { get; private set; }
        public event Action<bool>? AvailabilityChanged;

        public void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath) => CapturedSectionPath = sectionPath;

        public Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null) => Task.CompletedTask;

        public Task Initialize(WebApplication app) => Task.CompletedTask;
    }
}
