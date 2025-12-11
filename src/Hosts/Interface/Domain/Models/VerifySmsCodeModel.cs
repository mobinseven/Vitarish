namespace Vitarish.Interface.Domain.Models;

public class VerifySmsCodeModel : SendSmsCodeModel
{
    [AuthenticationCode]
    public string Code { get; set; }
}