using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Vorn.Aaas.Interface.Presentation.Pages.Account;

[ValidateAntiForgeryToken]
public sealed class PhoneLoginSubmitModel(
    IUsersService usersService,
    ISmsAuthenticationService smsAuthenticationService,
    SignInManager<User> signInManager,
    IUserStore<User> userStore,
    IStringLocalizer<Strings> localizer,
    ILogger<PhoneLoginSubmitModel> logger) : PageModel
{
    private readonly IUsersService _usersService = usersService;
    private readonly ISmsAuthenticationService _smsAuthenticationService = smsAuthenticationService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly IUserPhoneNumberStore<User> _phoneStore = (IUserPhoneNumberStore<User>)userStore;
    private readonly IStringLocalizer<Strings> _localizer = localizer;
    private readonly ILogger<PhoneLoginSubmitModel> _logger = logger;

    [BindProperty]
    public VerifyCodeModel Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        string failureDestination = ResolveFailureUrl();
        if(!IsInputValid())
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        if(!string.Equals(Input.Provider, TokenOptions.DefaultPhoneProvider, StringComparison.Ordinal))
        {
            LogWarning(null, "InvalidRequest");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        User? user = await _usersService.GetUserAsync(Input.UserId, HttpContext.RequestAborted);
        if(user is null)
        {
            LogWarning(null, "InvalidCredentials");
            return RedirectWithStatus("/Account/Login/Phone", BuildInvalidAttemptMessage());
        }

        string normalizedCode = Input.Code.ConvertToEnglishNumerals();
        if(!await _smsAuthenticationService.VerifyCode(user, normalizedCode))
        {
            LogWarning(user, "MfaFailure");
            return RedirectWithStatus(failureDestination, BuildInvalidAttemptMessage());
        }

        if(!await _phoneStore.GetPhoneNumberConfirmedAsync(user, HttpContext.RequestAborted))
        {
            await _phoneStore.SetPhoneNumberConfirmedAsync(user, true, HttpContext.RequestAborted);
        }

        await _signInManager.SignInAsync(user, true, "mfa");

        string? normalizedReturnUrl = NormalizeReturnUrl();
        if(string.IsNullOrEmpty(normalizedReturnUrl))
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
            "Phone MFA failed for {Username} ({UserId}) from {ClientIp}. Reason: {Reason}.",
            user?.UserName ?? Input.UserId ?? "unknown",
            user?.Id ?? Input.UserId ?? "unknown",
            HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason);

    private bool IsInputValid() =>
        !string.IsNullOrWhiteSpace(Input.UserId) &&
        !string.IsNullOrWhiteSpace(Input.Code);

    private string BuildInvalidAttemptMessage() => $"{_localizer["Error"]}: {_localizer["InvalidLoginAttempt"]}";

    private string ResolveFailureUrl()
    {
        if(string.IsNullOrWhiteSpace(Input.FailureUrl))
        {
            return "/Account/Login/Phone";
        }

        return WebUtility.UrlDecode(Input.FailureUrl);
    }

    private string? NormalizeReturnUrl()
    {
        if(string.IsNullOrWhiteSpace(Input.ReturnUrl))
        {
            return null;
        }

        return WebUtility.UrlDecode(Input.ReturnUrl);
    }

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
