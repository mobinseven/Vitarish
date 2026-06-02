using Interfaces.Base.Tests.TestInfrastructure;
using Microsoft.JSInterop;
using Vitarish.Interfaces.Base.Extensions;

namespace Interfaces.Base.Tests.Extensions;

public class JsModulesExtensionsTests
{
    [Fact]
    public async Task JsModulesExtensions_RegistersModulePath()
    {
        Mock<IJSRuntime> jsRuntime = new();
        string expectedPath = $"./_content/{NamespaceExtension.NamespaceToPath(typeof(SampleComponent))}/{nameof(SampleComponent)}.razor.js";

        _ = jsRuntime
            .Setup(runtime => runtime.InvokeAsync<IJSObjectReference>(
                "import",
                It.Is<object[]>(args => args.Length == 1
                    && args[0] != null
                    && string.Equals(args[0].ToString(), expectedPath, StringComparison.Ordinal))))
            .Returns(new ValueTask<IJSObjectReference>(Mock.Of<IJSObjectReference>()));

        SampleComponent component = new();
        _ = await JsModulesExtensions.RegisterJsModule(jsRuntime.Object, component);

        jsRuntime.VerifyAll();
    }
}
