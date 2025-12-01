using Vitarish.Web.Users.Domain.Attributes;

namespace Vitarish.Web.Users.Domain.Models;
public class VerifySmsCodeModel : SendSmsCodeModel
{
    [AuthenticationCode]
    public string Code { get; set; }
}