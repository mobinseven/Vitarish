using Vorn.Interface.Common;

public static class JsModulesExtensions
{
    public static ValueTask<IJSObjectReference> RegisterJsModule(this IJSRuntime js, object sender) => js.InvokeAsync<IJSObjectReference>(
                        "import",
                        $"./_content/{NamespaceExtension.NamespaceToPath(sender.GetType())}/{sender.GetType().Name}.razor.js");
}