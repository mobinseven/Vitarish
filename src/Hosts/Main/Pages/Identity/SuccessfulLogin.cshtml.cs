using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vitarish.Main.Pages.Identity;

public class SuccessfulLoginModel(IUsersService userService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Get authenticated user ID (or fail early)
        string userId = User.FindFirstValue("sub")
            ?? throw new InvalidOperationException("User id could not be retrieved from HttpContext.");

        // Fetch user
        User user = await userService.GetUserAsync(userId)
            ?? throw new InvalidOperationException("User could not be retrieved.");

        // If user has no name, force them to complete profile
        bool missingNames = string.IsNullOrWhiteSpace(user.FirstName)
                         || string.IsNullOrWhiteSpace(user.LastName);

        if (missingNames)
        {
            // Always redirect to local complete-name page.
            return RedirectToPage("/Settings/Name", new { ReturnUrl });
        }

        // If no return URL is provided, just go home
        if (string.IsNullOrEmpty(ReturnUrl))
        {
            return LocalRedirect("/");
        }

        // Decode and check safety of return URL
        string decoded = WebUtility.UrlDecode(ReturnUrl);

        // Only local URLs should be allowed back
        if (!Url.IsLocalUrl(decoded))
        {
            // Fallback to home if external or unsafe
            return LocalRedirect("/");
        }

        // Finally redirect to the intended local page
        return LocalRedirect(decoded);
    }
}
