using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vitarish.Interfaces.Accounts.Components;

namespace Vitarish.Main.Pages.Accounts;

[ValidateAntiForgeryToken]
public sealed class PasswordChangeSubmitModel(
    SignInManager<User> signInManager,
    ReturnUrlManager returnUrlManager,
    IStringLocalizer localizer,
    ILogger<PasswordChangeSubmitModel> logger) : PageModel
{
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly UserManager<User> _userManager = signInManager.UserManager;
    private readonly ReturnUrlManager _returnUrlManager = returnUrlManager;
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<PasswordChangeSubmitModel> _logger = logger;

    [BindProperty]
    public PasswordChangeModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = "/Account/Manage/Password";
        if (!ModelState.IsValid)
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        User? user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Login", BuildFailureMessage());
        }

        IdentityResult result;
        if (await _userManager.HasPasswordAsync(user))
        {
            if (string.IsNullOrWhiteSpace(Input.CurrentPassword))
            {
                LogWarning(user, "MissingCurrentPassword");
                return RedirectWithStatus(failureDestination, BuildFailureMessage());
            }

            bool currentPasswordValid = await _userManager.CheckPasswordAsync(user, Input.CurrentPassword);
            if (!currentPasswordValid)
            {
                LogWarning(user, "InvalidCurrentPassword");
                return RedirectWithStatus(failureDestination, BuildFailureMessage());
            }

            result = await _userManager.ChangePasswordAsync(
                user,
                Input.CurrentPassword,
                Input.NewPassword);
        }
        else
        {
            result = await _userManager.AddPasswordAsync(user, Input.NewPassword);
        }

        if (!result.Succeeded)
        {
            LogWarning(user, "UpdateFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        await _signInManager.RefreshSignInAsync(user);

        string destination = _returnUrlManager.ConsumeReturnUrl() ?? "/Account/Manage/Password";
        return RedirectWithStatus(destination, BuildSuccessMessage());
    }

    private string BuildFailureMessage() => $"{_localizer["Error"]}: {_localizer["PasswordSetUnsuccessful"]}";

    private string BuildSuccessMessage() => _localizer["PasswordSetSuccessful"];

    private IActionResult RedirectWithStatus(string destination, string message)
    {
        Response.Cookies.Append(
            IdentityRedirectManager.StatusCookieName,
            message,
            CreateStatusCookieOptions());
        return Redirect(destination);
    }

    private static CookieOptions CreateStatusCookieOptions() => new()
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
        MaxAge = TimeSpan.FromSeconds(5),
    };

    private void LogWarning(User? user, string reason) =>
        _logger.LogWarning(
            "Password change failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? "unknown",
            user?.Id ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
}
