using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Modules.Base.Core;

namespace Vitarish.Main.Tests;

public sealed class CompositionProfileTests
{
    [Fact]
    public void Product_evaluated_profile_includes_all_compiled_modules_and_apps()
    {
        IConfiguration configuration = Configuration(Module.ProductEvaluatedProfileIdentity);

        ModulesExtensions.GetModules(configuration).Select(module => module.Name)
            .Should().BeEquivalentTo([
                "Appointments", "Authentication", "Commerce", "Communications", "Contents", "Feed", "Files",
                "Financial", "Intelligence", "Journey", "Knowledge", "Links", "Medical", "Parties", "Platform",
                "Requests", "Sea", "Search", "Social", "Users"]);
        AppsExtensions.GetApps(configuration).Select(app => app.Name)
            .Should().BeEquivalentTo(["Clinic", "ProjectManagement", "Landing"]);
    }

    [Fact]
    public void Main_only_profile_retains_the_existing_restricted_composition()
    {
        IConfiguration configuration = Configuration(Module.MainOnlyEvaluatedProfileIdentity);

        ModulesExtensions.GetModules(configuration).Select(module => module.Name)
            .Should().BeEquivalentTo(["Authentication", "Requests", "Users", "Parties", "Platform", "Feed"]);
        AppsExtensions.GetApps(configuration).Should().BeEmpty();
    }

    [Fact]
    public void Unknown_evaluated_profile_fails_closed()
    {
        IConfiguration configuration = Configuration("unknown-profile");

        Action getModules = () => ModulesExtensions.GetModules(configuration);
        Action getApps = () => AppsExtensions.GetApps(configuration);

        getModules.Should().Throw<InvalidOperationException>();
        getApps.Should().Throw<InvalidOperationException>();
    }

    private static IConfiguration Configuration(string identity) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            [Module.EvaluatedConfigurationEnabledKey] = "true",
            [Module.EvaluatedConfigurationIdentityKey] = identity
        })
        .Build();
}
