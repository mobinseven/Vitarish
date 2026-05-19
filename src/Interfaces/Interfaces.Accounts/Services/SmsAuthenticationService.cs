using Microsoft.EntityFrameworkCore;

public interface ISmsAuthenticationService
{
    Task SendCode(User user, string phonenumber);
    Task<bool> VerifyCode(User user, string code);
    Task<User> GetOrRegisterUser(string phoneNumber);
}

public class SmsAuthenticationService(
        UserManager<User> userManager,
        ILogger<SmsAuthenticationService> logger,
        ICommunicationCommandQueue communicationCommandQueue) : ISmsAuthenticationService
{
    public async Task SendCode(User user, string phonenumber)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (string.IsNullOrWhiteSpace(phonenumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phonenumber));
        }

        string code = await userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider);
        communicationCommandQueue.Enqueue(new SmsCodeCommunicationCommand(phonenumber, code));
    }

    public async Task<bool> VerifyCode(User user, string code) => await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, code);

    public async Task<User> GetOrRegisterUser(string phoneNumber)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user is not null)
        {
            return user;
        }

        user = Activator.CreateInstance<User>();
        _ = await userManager.SetUserNameAsync(user, phoneNumber);
        _ = await userManager.SetPhoneNumberAsync(user, phoneNumber);
        IdentityResult? result = await userManager.CreateAsync(user);
        return !result.Succeeded ? throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}"))) : user;
    }
}
