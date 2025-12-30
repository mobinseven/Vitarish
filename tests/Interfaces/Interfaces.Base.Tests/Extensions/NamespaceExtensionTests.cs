using Interfaces.Base.Tests.TestInfrastructure;
using Vitarish.Interfaces.Base.Extensions;

namespace Interfaces.Base.Tests.Extensions;

public class NamespaceExtensionTests
{
    [Fact]
    public void NamespaceExtension_ToNamespace_ReturnsExpectedNamespace()
    {
        string result = NamespaceExtension.NamespaceToPath(typeof(SampleComponent));

        result.Should().Be("Interfaces.Base.Tests/TestInfrastructure");
    }
}
