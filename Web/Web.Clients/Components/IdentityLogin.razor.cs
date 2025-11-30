using Microsoft.AspNetCore.Components;

namespace Vitarish.Web.Clients;

/// <summary>
/// Component for rendering a login form.
/// </summary>
public partial class IdentityLogin
{
    /// <summary>
    /// The submit button content.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment SubmitButton { get; set; } = default!;

    /// <summary>
    /// Optional return URL after login.
    /// </summary>
    [Parameter]
    public string? ReturnUrl { get; set; }
}
