using FluentAssertions;
using Xunit;

namespace Vitarish.Clinic.Application.Tests.ClinicalEvents;

public sealed class ClinicalEventArchitectureTests
{
    [Fact]
    public void Clinic_application_has_no_sea_or_redis_reference()
    {
        string project = File.ReadAllText(
            RepositoryPath(
                "src/Apps/Clinic/Application/Clinic.Application.csproj"));

        project.Should().NotContain("Modules\\Sea")
            .And.NotContain("Modules/Sea")
            .And.NotContain("StackExchange.Redis");
    }

    [Fact]
    public void Clinical_event_proposals_do_not_add_database_schema()
    {
        File.ReadAllText(
                RepositoryPath("src/Apps/Clinic/Infrastructure/Data/ClinicDbContext.cs"))
            .Should().NotContain("ClinicalEventProposal");
        File.ReadAllText(
                RepositoryPath("src/Apps/Clinic/Infrastructure/Views/views.pgsql"))
            .Should().NotContain("ClinicalEventProposal");
    }

    private static string RepositoryPath(string relative)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Vitarish.slnx")))
            directory = directory.Parent;
        return Path.Combine(
            directory?.FullName ??
            throw new InvalidOperationException("Repository root not found."),
            relative);
    }
}
