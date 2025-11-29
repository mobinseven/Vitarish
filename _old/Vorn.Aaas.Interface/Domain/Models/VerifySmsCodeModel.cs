using Vorn.Aaas.Interface.Domain.Attributes;

namespace Vorn.Aaas.Interface.Domain.Models;
public class VerifySmsCodeModel : SendSmsCodeModel
{
    [AuthenticationCode]
    public string Code { get; set; }
}