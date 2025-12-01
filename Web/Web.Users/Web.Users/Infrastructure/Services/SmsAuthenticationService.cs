using Microsoft.EntityFrameworkCore;
public interface ISmsAuthenticationService
{
    Task SendCode(User user, string phonenumber);
    Task<bool> VerifyCode(User user, string code);
    Task<User> GetOrRegisterUser(string phoneNumber);
}
internal class SmsAuthenticationService(
        UserManager<User> userManager,
        ILogger<SmsAuthenticationService> logger,
        IMessageQueue messageQueue) : ISmsAuthenticationService
{
    public async Task SendCode(User user, string phonenumber)
    {
        string code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        messageQueue.EnqueueSms(phonenumber, code);
    }
    public async Task<bool> VerifyCode(User user, string code) => await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, code);
    public async Task<User> GetOrRegisterUser(string phoneNumber)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if(user is not null)
        {
            return user;
        }
        user = Activator.CreateInstance<User>();
        await userManager.SetUserNameAsync(user, phoneNumber);
        await userManager.SetPhoneNumberAsync(user, phoneNumber);
        IdentityResult? result = await userManager.CreateAsync(user);
        if(!result.Succeeded)
        {
            throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
        return user;
    }

}
