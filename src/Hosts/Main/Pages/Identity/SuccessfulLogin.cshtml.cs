using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vitarish.Main.Pages.Identity;

public class SuccessfulLoginModel(IUsersService userService) : PageModel
{
    private readonly IUsersService _userService = userService;


    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }


    public async Task<IActionResult> OnGetAsync()
    {
        string? id = User.FindFirstValue("sub") ?? throw new InvalidOperationException("/Account/SuccessfulLogin user could not be retrived from HttpContext");


        User user = await _userService.GetUserAsync(id)
        ?? throw new InvalidOperationException("/Account/SuccessfulLogin user could not be retrived from HttpContext");


        bool missingNames = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName);


        if (missingNames)
        {
            if (string.IsNullOrWhiteSpace(ReturnUrl))
            {
                return Redirect("/Settings/Name");
            }
            else
            {
                return Redirect(ReturnUrl);
            }
        }


        string? target = WebUtility.UrlDecode(ReturnUrl);
        return !string.IsNullOrWhiteSpace(target) && Url.IsLocalUrl(target) ? LocalRedirect(target) : Redirect("/");
    }
}
