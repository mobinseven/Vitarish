namespace Vitarish.Accounts.Domain.Models;

public class SendSmsCodeModel
{
    public User User { get; set; }
    [PhoneNumber]
    public string PhoneNumber { get; set; } = "";
}
