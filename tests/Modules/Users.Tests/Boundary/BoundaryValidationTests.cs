using System.Xml.Linq;
using FluentAssertions;

namespace Users.Tests.Boundary;

public class BoundaryValidationTests
{
    [Fact]
    public void Interface_project_references_only_client_and_contracts()
    {
        var csprojPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src", "Modules", "Users", "Interface", "Users.Interface.csproj");
        var doc = XDocument.Load(csprojPath);
        var references = doc.Descendants("ProjectReference")
            .Select(r => r.Attribute("Include")?.Value ?? "")
            .ToList();

        references.Should().NotContain(r => r.Contains("Application"), "Interface should not reference Application");
        references.Should().NotContain(r => r.Contains("Domain"), "Interface should not reference Domain");
        references.Should().NotContain(r => r.Contains("Infrastructure"), "Interface should not reference Infrastructure");
        references.Should().NotContain(r => r.Contains("Api"), "Interface should not reference Api");
    }
}
