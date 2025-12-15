namespace Vitarish.Clients.Interface.Identity.Components;

public partial class Login
{

    [Parameter, EditorRequired] public RenderFragment SubmitButton { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

}
