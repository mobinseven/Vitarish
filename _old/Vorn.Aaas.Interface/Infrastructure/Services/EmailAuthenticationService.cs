using Microsoft.EntityFrameworkCore;

namespace Vorn.Aaas.Interface.Infrastructure.Services;
public interface IEmailAuthenticationService
{
    Task SendCode(User user, string email);
    Task<bool> VerifyCode(User user, string code);
    Task<User> GetOrRegisterUser(string email);
}
internal class EmailAuthenticationService(
        UserManager<User> userManager,
        ILogger<SmsAuthenticationService> logger,
        IMessageQueue messageQueue) : IEmailAuthenticationService
{
    public async Task SendCode(User user, string email)
    {
        string code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);
        (string HtmlBody, string TextBody) body = GetAuthenticationCodeEmail(user.UserName, code);
        messageQueue.EnqueueEmail(email, "Code", body.TextBody, body.HtmlBody);
    }
    public async Task<bool> VerifyCode(User user, string code) => await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, code);
    public async Task<User> GetOrRegisterUser(string email)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == email);
        if(user is not null)
        {
            return user;
        }
        user = Activator.CreateInstance<User>();
        await userManager.SetUserNameAsync(user, email);
        await userManager.SetEmailAsync(user, email);
        IdentityResult? result = await userManager.CreateAsync(user);
        if(!result.Succeeded)
        {
            throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
        return user;
    }
    public (string HtmlBody, string TextBody) GetAuthenticationCodeEmail(string userName, string authCode)
    {
        if(string.IsNullOrWhiteSpace(userName))
        {
            userName = "User";
        }

        // Search-filter friendly HTML
        string html = $@"<!DOCTYPE html>
<html>
<head><meta charset=""UTF-8""></head>
<body style=""font-family:Arial,sans-serif;color:#333;margin:0;padding:0;"">
  <div style=""max-width:600px;margin:auto;padding:20px;"">
    <h2>Dear {System.Net.WebUtility.HtmlEncode(userName)},</h2>
    <p>We received a request to sign in to your account.</p>
    <p>Your authentication code is:</p>
    <p style=""font-size:24px;font-weight:bold;color:#2c3e50;"">
      {System.Net.WebUtility.HtmlEncode(authCode)}
    </p>
    <p>This code is valid for a limited time. Please do not share it.</p>
    <br/>
    <p>If you did not request this code, please contact our support team.</p>
    <p>Thank you,<br/>The Vitarish Security Team</p>
  </div>
</body>
</html>";

        // Plain-text alternative
        string text = $@"Dear {userName},

We received a request to sign in to your account.

Your authentication code is: {authCode}

This code is valid for a limited time. Please do not share it with anyone.

If you did not request this code, please contact our support team.

Thank you,
The Vitarish Security Team";

        return (HtmlBody: html, TextBody: text);
    }

}
