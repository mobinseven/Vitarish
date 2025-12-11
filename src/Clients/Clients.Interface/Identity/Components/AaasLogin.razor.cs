namespace Vitarish.Client;

public partial class AaasLogin
{

    [Parameter, EditorRequired] public RenderFragment SubmitButton { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

}
