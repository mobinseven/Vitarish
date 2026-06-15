using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Main.Infrastructure.Options;
using Xunit;

namespace Hosts.Main.Tests;

public class ApplicationOptionsExtensionsTests
{
    [Fact]
    public void AddApplicationOptions_DoesNotDiscoverModulesFromServiceCollection()
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        CapturingModule module = new("Example");
        services.AddSingleton<IReadOnlyList<Vitarish.Modules.Base.IModule>>([module]);

        _ = services.AddApplicationOptions(configuration);

        module.CapturedSectionPath.Should().BeNull();
    }

    [Fact]
    public async Task ModuleRegister_DelegatesOptionBindingThroughApplicationModulePath()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        CapturingModule module = new("Example");

        await module.Register(builder);

        module.CapturedSectionPath.Should().Be($"{ApplicationOptions.Section}:Example");
    }

    [Fact]
    public void ApplicationOptionsSection_UsesSharedModuleApplicationSection()
    {
        ApplicationOptions.Section.Should().Be(Vitarish.Modules.Base.Module.ApplicationSection);
    }

    private sealed class CapturingModule(string name) : Vitarish.Modules.Base.Module
    {
        public override string Name { get; } = name;
        public string? CapturedSectionPath { get; private set; }

        public override void RegisterOptions(IServiceCollection services, IConfiguration configuration, string sectionPath) => CapturedSectionPath = sectionPath;

        public override Task Register(WebApplicationBuilder builder, Action<DbContextOptionsBuilder>? configureDbContext = null)
        {
            RegisterOptions(builder.Services, builder.Configuration, OptionsSectionPath);
            return Task.CompletedTask;
        }

        public override Task Initialize(WebApplication app) => Task.CompletedTask;
    }
}
