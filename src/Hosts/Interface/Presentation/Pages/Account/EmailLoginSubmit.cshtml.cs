using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Interface.Presentation.Pages.Account;

[ValidateAntiForgeryToken]
public sealed class EmailLoginSubmitModel(
    IUsersService usersService,
    IEmailAuthenticationService emailAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    IStringLocalizer<Strings> localizer,
    ILogger<EmailLoginSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly IEmailAuthenticationService _emailAuthenticationService = emailAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserEmailStore<User> _emailStore = (IUserEmailStore<User>)userStore;
    private readonly IStringLocalizer<Strings> _localizer = localizer;
    private readonly ILogger<EmailLoginSubmitModel> _logger = logger;

    [BindProperty]
    public VerifyCodeModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = ResolveFailureUrl();
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

        string? normalizedReturnUrl = NormalizeReturnUrl();
        if (string.IsNullOrEmpty(normalizedReturnUrl))
        {
            return Redirect("/Account/SuccessfulLogin");
        }

        string redirectUri = QueryHelpers.AddQueryString(
            "/Account/SuccessfulLogin",
            new Dictionary<string, string?>
            {
                [nameof(Input.ReturnUrl)] = normalizedReturnUrl,
            });

        return Redirect(redirectUri);
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

    private string ResolveFailureUrl() => string.IsNullOrWhiteSpace(Input.FailureUrl) ? "/Account/Login/Email" : WebUtility.UrlDecode(Input.FailureUrl);

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
}
