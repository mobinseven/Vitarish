using Xunit;
using FluentAssertions;

namespace Vitarish.Clinic.Application.Tests.ClinicalEvents;

public sealed class ClinicalEventArchitectureTests
{
    [Fact]
    public void Clinic_application_has_no_sea_reference()
    {
        string project = File.ReadAllText(RepositoryPath("src/Apps/Clinic/Application/Clinic.Application/Clinic.Application.csproj"));
        project.Should().NotContain("Modules\\Sea").And.NotContain("Modules/Sea");
    }

    [Fact]
    public void Clinical_event_agent_artifacts_are_clinic_owned_and_absent_from_sea_workflow_layers()
    {
        Directory.EnumerateFiles(RepositoryPath("src/Apps/Clinic/Application/Clinic.Application"), "*ClinicalEvent*.cs", SearchOption.AllDirectories)
            .Should().NotBeEmpty();
        string[] forbidden = ["Application", "Domain", "Infrastructure", "Interface"];
        forbidden.Select(layer => RepositoryPath($"src/Modules/Sea/{layer}"))
            .Where(Directory.Exists)
            .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains("ClinicalEvent", StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    [Fact]
    public void Sea_projects_do_not_reference_clinic()
    {
        Directory.EnumerateFiles(RepositoryPath("src/Modules/Sea"), "*.csproj", SearchOption.AllDirectories)
            .Select(File.ReadAllText).Should().OnlyContain(text => !text.Contains("Apps\\Clinic", StringComparison.Ordinal) && !text.Contains("Apps/Clinic", StringComparison.Ordinal));
    }

    private static string RepositoryPath(string relative)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Vitarish.slnx"))) directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }
}
