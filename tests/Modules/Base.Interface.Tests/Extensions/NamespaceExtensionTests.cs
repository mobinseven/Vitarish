using Modules.Base.Interface.Tests.TestInfrastructure;
using Vitarish.Modules.Base.Interface.Extensions;

namespace Modules.Base.Interface.Tests.Extensions;

public class NamespaceExtensionTests
{
    [Fact]
    public void NamespaceExtension_ToNamespace_ReturnsExpectedNamespace()
    {
        string result = NamespaceExtension.NamespaceToPath(typeof(SampleComponent));

        _ = result.Should().Be("Modules.Base.Interface.Tests/TestInfrastructure");
    }
}
