using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vorn.Aaas.Users.Presentation.Abstractions;

namespace Vitarish.Web.Users.Client.Pages.Account;

public class SuccessfulLoginModel : PageModel
{
    private readonly IUsersService _userService;


    public SuccessfulLoginModel(IUsersService userService) => _userService = userService;


    [BindProperty(SupportsGet = true)]
    public string? FailureUrl { get; set; }


    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }


    public async Task<IActionResult> OnGetAsync()
    {
        string? id = User.FindFirstValue("sub");
        if(id is null)
        {
            throw new InvalidOperationException("/Account/SuccessfulLogin user could not be retrieved from HttpContext");
        }


        User user = await _userService.GetUserAsync(id)
        ?? throw new InvalidOperationException("/Account/SuccessfulLogin user could not be retrieved from HttpContext");


        bool missingNames = string.IsNullOrWhiteSpace(user.FirstName) && string.IsNullOrWhiteSpace(user.LastName);


        if(missingNames)
        {
            if(string.IsNullOrWhiteSpace(ReturnUrl))
            {
                return Redirect("/Account/Manage");
            }
            else
            {
                string encoded = Uri.EscapeDataString(ReturnUrl);
                return Redirect($"/Account/Manage?ReturnUrl={encoded}");
            }
        }


        string? target = WebUtility.UrlDecode(ReturnUrl);
        if(!string.IsNullOrWhiteSpace(target) && Url.IsLocalUrl(target))
        {
            return LocalRedirect(target);
        }


        return Redirect("/");
    }
}