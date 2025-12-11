
namespace Vitarish.Accounts.Domain.Models;

public class VerifySmsCodeModel : SendSmsCodeModel
{
    [AuthenticationCode]
    public string Code { get; set; }
}
