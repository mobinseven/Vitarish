using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vitarish.Interfaces.Base.Extensions;

namespace Vitarish.Main.Pages.Identity;

[ValidateAntiForgeryToken]
public sealed class EmailLoginSubmitModel(
    IUsersService usersService,
    IEmailAuthenticationService emailAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    ReturnUrlManager returnUrlManager,
    IStringLocalizer localizer,
    ILogger<EmailLoginSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly IEmailAuthenticationService _emailAuthenticationService = emailAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserEmailStore<User> _emailStore = (IUserEmailStore<User>)userStore;
    private readonly ReturnUrlManager _returnUrlManager = returnUrlManager;
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<EmailLoginSubmitModel> _logger = logger;

    [BindProperty]
    public VerifyCodeModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = "/Account/Login/Email";
        if (!IsInputValid())
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        if (!string.Equals(Input.Provider, TokenOptions.DefaultEmailProvider, StringComparison.Ordinal))
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        User? user = await _usersService.GetUserAsync(Input.UserId, HttpContext.RequestAborted);
        if (user is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Login/Email", BuildInvalidAttemptMessage());
        }

        string normalizedCode = Input.Code.ConvertToEnglishNumerals();
        if (!await _emailAuthenticationService.VerifyCode(user, normalizedCode))
        {
            LogWarning(user, "MfaFailure");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        if (!await _emailStore.GetEmailConfirmedAsync(user, HttpContext.RequestAborted))
        {
            await _emailStore.SetEmailConfirmedAsync(user, true, HttpContext.RequestAborted);
        }

        await _signInManager.SignInAsync(user, true, "mfa");

        // ReturnUrl is already in the cookie, just redirect to SuccessfulLogin
        return Redirect("/Account/SuccessfulLogin");
    }

    private void LogWarning(User? user, string reason) =>
        _logger.LogWarning(
            "Email MFA failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? Input.UserId ?? "unknown",
            user?.Id ?? Input.UserId ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);

    private bool IsInputValid() =>
        !string.IsNullOrWhiteSpace(Input.UserId) &&
        !string.IsNullOrWhiteSpace(Input.Code);

    private string BuildInvalidAttemptMessage() => $"{_localizer["Error"]}: {_localizer["InvalidLoginAttempt"]}";

    private IActionResult RedirectWithStatus(string destination, string message)
    {
        Response.Cookies.Append(
            IdentityCookieNames.StatusCookieName,
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
}
