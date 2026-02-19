using Microsoft.EntityFrameworkCore;

namespace Vitarish.Interfaces.Accounts.Services;

public interface IEmailAuthenticationService
{
    Task SendCode(User user, string email);
    Task<bool> VerifyCode(User user, string code);
    Task<User> GetOrRegisterUser(string email);
}

public class EmailAuthenticationService(
        UserManager<User> userManager,
        ILogger<EmailAuthenticationService> logger,
        IMessageQueue messageQueue) : IEmailAuthenticationService
{
    public async Task SendCode(User user, string email)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        string code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        (string HtmlBody, string TextBody) = GetAuthenticationCodeEmail(user.UserName, code);
        messageQueue.EnqueueEmail(email, "Code", TextBody, HtmlBody);
    }

    public async Task<bool> VerifyCode(User user, string code) => await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, code);

    public async Task<User> GetOrRegisterUser(string email)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is not null)
        {
            return user;
        }

        user = Activator.CreateInstance<User>();
        _ = await userManager.SetUserNameAsync(user, email);
        _ = await userManager.SetEmailAsync(user, email);
        IdentityResult? result = await userManager.CreateAsync(user);
        return !result.Succeeded ? throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}"))) : user;
    }

    public static (string HtmlBody, string TextBody) GetAuthenticationCodeEmail(string userName, string authCode)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = "User";
        }

        string encodedUserName = System.Net.WebUtility.HtmlEncode(userName);
        string encodedAuthCode = System.Net.WebUtility.HtmlEncode(authCode);

        string html = $"""
<!DOCTYPE html>
<html>
<head><meta charset="UTF-8"></head>
<body style="font-family:Arial,sans-serif;color:#333;margin:0;padding:0;">
  <div style="max-width:600px;margin:auto;padding:20px;">
    <h2>Dear {encodedUserName},</h2>
    <p>We received a request to sign in to your account.</p>
    <p>Your authentication code is:</p>
    <p style="font-size:24px;font-weight:bold;color:#2c3e50;">
      {encodedAuthCode}
    </p>
    <p>This code is valid for a limited time. Please do not share it.</p>
    <br/>
    <p>If you did not request this code, please contact our support team.</p>
    <p>Thank you,<br/>The Vitarish Security Team</p>
  </div>
</body>
</html>
""";

        string text = $"""
Dear {userName},

We received a request to sign in to your account.

Your authentication code is: {authCode}

This code is valid for a limited time. Please do not share it with anyone.

If you did not request this code, please contact our support team.

Thank you,
The Vitarish Security Team
""";

        return (HtmlBody: html, TextBody: text);
    }
}
