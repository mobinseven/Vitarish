using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vitarish.Modules.Aggregator.Interface.Extensions;

namespace Vitarish.Main.Pages.Accounts;

[ValidateAntiForgeryToken]
public sealed class EmailChangeSubmitModel(
    IUsersService usersService,
    IEmailAuthenticationService emailAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    ReturnUrlManager returnUrlManager,
    IStringLocalizer localizer,
    ILogger<EmailChangeSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly IEmailAuthenticationService _emailAuthenticationService = emailAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserEmailStore<User> _emailStore =
        userStore as IUserEmailStore<User> ??
        throw new NotSupportedException("The user store must implement IUserEmailStore<User>.");
    private readonly ReturnUrlManager _returnUrlManager = returnUrlManager;
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<EmailChangeSubmitModel> _logger = logger;

    [BindProperty]
    public EmailChangeVerifyModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = "/Account/Manage/Email";
        if (!ModelState.IsValid || !IsInputValid())
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        if (!string.Equals(Input.Provider, TokenOptions.DefaultEmailProvider, StringComparison.Ordinal))
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        User? currentUser = await _signInManager.UserManager.GetUserAsync(User);
        if (currentUser is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Manage/Email", BuildFailureMessage());
        }

        if (!string.IsNullOrWhiteSpace(Input.UserId) && !string.Equals(Input.UserId, currentUser.Id, StringComparison.Ordinal))
        {
            LogWarning(currentUser, "UserMismatch");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        string normalizedCode = Input.Code.ConvertToEnglishNumerals();
        if (!await _emailAuthenticationService.VerifyCode(currentUser, normalizedCode))
        {
            LogWarning(currentUser, "MfaFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        IdentityResult setEmailResult = await _usersService.SetUserEmailAsync(currentUser.Id, Input.Email, HttpContext.RequestAborted);
        if (!setEmailResult.Succeeded)
        {
            LogWarning(currentUser, "UpdateFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        await ConfirmEmailAsync(currentUser);
        await _signInManager.SignInAsync(currentUser, true, "mfa");

        string destination = _returnUrlManager.ConsumeReturnUrl() ?? "/Account/Manage";
        return RedirectWithStatus(destination, BuildSuccessMessage());
    }

    private async Task ConfirmEmailAsync(User user)
    {
        if (!await _emailStore.GetEmailConfirmedAsync(user, HttpContext.RequestAborted))
        {
            await _emailStore.SetEmailConfirmedAsync(user, true, HttpContext.RequestAborted);
        }
    }

    private bool IsInputValid() =>
        !string.IsNullOrWhiteSpace(Input.Code) &&
        !string.IsNullOrWhiteSpace(Input.Email);

    private string BuildFailureMessage() => $"{_localizer["Error"]}: {_localizer["EmailChangeFailed"]}";

    private string BuildSuccessMessage() => _localizer["EmailChangeSuccess"];

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

    private void LogWarning(User? user, string reason) =>
        _logger.LogWarning(
            "Email change verification failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? Input.UserId ?? "unknown",
            user?.Id ?? Input.UserId ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
}
