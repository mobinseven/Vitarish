using System.Xml.Linq;
using FluentAssertions;

namespace Parties.Tests.Boundary;

public class BoundaryValidationTests
{
    [Fact]
    public void Interface_project_references_only_client_and_contracts()
    {
        var csprojPath = FindRepoRelativeCsproj("src", "Modules", "Parties", "Interface", "Parties.Interface.csproj");
        var doc = XDocument.Load(csprojPath);
        var references = doc.Descendants("ProjectReference")
            .Select(r => r.Attribute("Include")?.Value ?? "")
            .ToList();

        references.Should().NotContain(r => r.Contains("Application"), "Interface should not reference Application");
        references.Should().NotContain(r => r.Contains("Domain"), "Interface should not reference Domain");
        references.Should().NotContain(r => r.Contains("Infrastructure"), "Interface should not reference Infrastructure");
        references.Should().NotContain(r => r.Contains("Api"), "Interface should not reference Api");
    }

    private static string FindRepoRelativeCsproj(params string[] pathParts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.GetFiles("*.slnx").Any() && !dir.GetFiles("*.sln").Any())
            dir = dir.Parent;
        dir.Should().NotBeNull("repository root should be reachable from test output");
        return Path.Combine(dir!.FullName, Path.Combine(pathParts));
    }
}
