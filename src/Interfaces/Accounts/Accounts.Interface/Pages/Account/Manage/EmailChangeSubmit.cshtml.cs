using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Interface.Localization;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Accounts.Interface.Pages.Account.Manage;

[ValidateAntiForgeryToken]
public sealed class EmailChangeSubmitModel(
    IUsersService usersService,
    IEmailAuthenticationService emailAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    IStringLocalizer localizer,
    ILogger<EmailChangeSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly IEmailAuthenticationService _emailAuthenticationService = emailAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserEmailStore<User> _emailStore = (IUserEmailStore<User>)userStore;
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<EmailChangeSubmitModel> _logger = logger;

    [BindProperty]
    public EmailChangeVerifyModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = ResolveFailureUrl();
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

        User? user = await _usersService.GetUserAsync(Input.UserId, HttpContext.RequestAborted);
        if (user is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Manage/Email", BuildFailureMessage());
        }

        string normalizedCode = Input.Code.ConvertToEnglishNumerals();
        if (!await _emailAuthenticationService.VerifyCode(user, normalizedCode))
        {
            LogWarning(user, "MfaFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        IdentityResult setEmailResult = await _usersService.SetUserEmailAsync(user.Id, Input.Email, HttpContext.RequestAborted);
        if (!setEmailResult.Succeeded)
        {
            LogWarning(user, "UpdateFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        await ConfirmEmailAsync(user);
        await _signInManager.SignInAsync(user, true, "mfa");

        string destination = NormalizeReturnUrl() ?? "/Account/Manage";
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
        !string.IsNullOrWhiteSpace(Input.UserId) &&
        !string.IsNullOrWhiteSpace(Input.Code) &&
        !string.IsNullOrWhiteSpace(Input.Email);

    private string BuildFailureMessage() => $"{_localizer["Error"]}: {_localizer["EmailChangeFailed"]}";

    private string BuildSuccessMessage() => _localizer["EmailChangeSuccess"]; 

    private string ResolveFailureUrl() => string.IsNullOrWhiteSpace(Input.FailureUrl) ? "/Account/Manage/Email" : WebUtility.UrlDecode(Input.FailureUrl);

    private string? NormalizeReturnUrl() => string.IsNullOrWhiteSpace(Input.ReturnUrl) ? null : WebUtility.UrlDecode(Input.ReturnUrl);

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
            "Email change verification failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? Input.UserId ?? "unknown",
            user?.Id ?? Input.UserId ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
}
