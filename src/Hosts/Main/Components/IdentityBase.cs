namespace Vitarish.Main.Components;

public class IdentityBase : IdentityCommonBase
{
    [CascadingParameter] protected HttpContext? HttpContext { get; set; }
}
