using Microsoft.AspNetCore.Components;
namespace Vorn.Aaas.Client;

public partial class AaasLogin
{

    [Parameter, EditorRequired] public RenderFragment SubmitButton { get; set; } = default!;
    [Parameter] public string? ReturnUrl { get; set; }

}
