using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Net;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Interface.Localization;
using Vitarish.Interfaces.Base.Extensions;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Main.Pages.Accounts;

[ValidateAntiForgeryToken]
public sealed class PhoneChangeSubmitModel(
    IUsersService usersService,
    ISmsAuthenticationService smsAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    IStringLocalizer localizer,
    ILogger<PhoneChangeSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly ISmsAuthenticationService _smsAuthenticationService = smsAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserPhoneNumberStore<User> _phoneStore =
        userStore as IUserPhoneNumberStore<User> ??
        throw new NotSupportedException("The user store must implement IUserPhoneNumberStore<User>.");
    private readonly IStringLocalizer _localizer = localizer;
    private readonly ILogger<PhoneChangeSubmitModel> _logger = logger;

    [BindProperty]
    public PhoneChangeVerifyModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = ResolveFailureUrl();
        if (!ModelState.IsValid || !IsInputValid())
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        if (!string.Equals(Input.Provider, TokenOptions.DefaultPhoneProvider, StringComparison.Ordinal))
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        User? currentUser = await _signInManager.UserManager.GetUserAsync(User);
        if (currentUser is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Login", BuildFailureMessage());
        }

        if (!string.IsNullOrWhiteSpace(Input.UserId) && !string.Equals(Input.UserId, currentUser.Id, StringComparison.Ordinal))
        {
            LogWarning(currentUser, "UserMismatch");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        string normalizedCode = Input.Code.ConvertToEnglishNumerals();
        if (!await _smsAuthenticationService.VerifyCode(currentUser, normalizedCode))
        {
            LogWarning(currentUser, "MfaFailure");
            return RedirectWithStatus(failureDestination, $"{_localizer["Error"]}: {_localizer["WrongCode"]}");
        }

        string normalizedPhone = Input.PhoneNumber.ConvertToEnglishNumerals();
        IdentityResult setPhoneResult = await _usersService.SetUserPhoneNumberAsync(
            currentUser.Id,
            normalizedPhone,
            HttpContext.RequestAborted);
        if (!setPhoneResult.Succeeded)
        {
            LogWarning(currentUser, "UpdateFailure");
            return RedirectWithStatus(failureDestination, BuildFailureMessage());
        }

        await _phoneStore.SetPhoneNumberConfirmedAsync(currentUser, true, HttpContext.RequestAborted);

        await _signInManager.RefreshSignInAsync(currentUser);

        string destination = NormalizeReturnUrl() ?? "/Account/Manage/Phone";
        return RedirectWithStatus(destination, BuildSuccessMessage());
    }

    private bool IsInputValid() =>
        !string.IsNullOrWhiteSpace(Input.Code) &&
        !string.IsNullOrWhiteSpace(Input.PhoneNumber);

    private string BuildFailureMessage() => $"{_localizer["Error"]}: {_localizer["PhoneChangeFailed"]}";

    private string BuildSuccessMessage() => _localizer["PhoneChangeSuccess"];

    private string ResolveFailureUrl() => string.IsNullOrWhiteSpace(Input.FailureUrl) ? "/Account/Manage/Phone" : WebUtility.UrlDecode(Input.FailureUrl);

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
            "Phone change verification failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? Input.UserId ?? "unknown",
            user?.Id ?? Input.UserId ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);
}
