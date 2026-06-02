namespace Vitarish.Interfaces.Accounts.Models;

public class SendSmsCodeModel
{
    public required User User { get; set; }
    [PhoneNumber]
    public string PhoneNumber { get; set; } = "";
}
