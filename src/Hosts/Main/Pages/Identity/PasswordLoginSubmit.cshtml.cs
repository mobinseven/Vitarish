
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vitarish.Main.Pages.Identity;

[ValidateAntiForgeryToken]
public sealed class PasswordLoginSubmitModel(
    IUsersService usersService,
    SignInManager<User> signInManager,
    ReturnUrlManager returnUrlManager,
    IStringLocalizer localizer,
    ILogger<PasswordLoginSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly ReturnUrlManager _returnUrlManager = returnUrlManager;
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<PasswordLoginSubmitModel> _logger = logger;

    [BindProperty]
    public PasswordLoginModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = "/Account/Login/Password";
        if (!ModelState.IsValid)
        {
            LogWarning(null, Input.Username, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        User? user = await _usersService.GetUserByNameAsync(Input.Username, HttpContext.RequestAborted);
        if (user is null)
        {
            LogWarning(null, Input.Username, "InvalidCredentials");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(user, Input.Password, true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            // ReturnUrl is already in the cookie, just redirect to SuccessfulLogin
            return Redirect("/Account/SuccessfulLogin");
        }

        if (result.IsLockedOut)
        {
            LogWarning(user, Input.Username, "Lockout");
            return Redirect("/Account/Lockout");
        }

        LogWarning(user, Input.Username, result.IsNotAllowed ? "AccessDenied" : "InvalidCredentials");
        return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
    }

    private static CookieOptions CreateStatusCookieOptions() => new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    private IActionResult RedirectWithStatus(string destination, string message)
    {
        Response.Cookies.Append(
            IdentityCookieNames.StatusCookieName,
            message,
            CreateStatusCookieOptions());
        return Redirect(destination);
    }

    private string BuildInvalidAttemptMessage() => $"{_localizer["Error"]}: {_localizer["InvalidLoginAttempt"]}";

    private void LogWarning(User? user, string? username, string reason) =>
        _logger.LogWarning(
            "Password login failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            username ?? user?.UserName ?? "unknown",
            user?.Id ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
}
