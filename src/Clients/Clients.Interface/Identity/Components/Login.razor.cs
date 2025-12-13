namespace Vitarish.Client;

public partial class Login
{

    [Parameter, EditorRequired] public RenderFragment SubmitButton { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

}
